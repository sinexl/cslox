using System.Diagnostics;
using cslox.Ast.Generated;

namespace cslox.Runtime;

public class Interpreter : IExpressionVisitor<object?>, IStatementVisitor<Unit>
{
    public Interpreter()
    {
        Locals = new Dictionary<Expression, int>();
        Globals = new ExecutionContext();
        Context = Globals;
        Globals.Define("clock", new DotnetFunction(0, () => DateTimeOffset.Now.ToUnixTimeMilliseconds() / 1000.0));
    }

    public ExecutionContext Globals { get; }
    public ExecutionContext Context { get; set; }

    public Dictionary<Expression, int> Locals { get; set; }

    object? IExpressionVisitor<object?>.Visit<TExpression>(TExpression expression) => Evaluate(expression);

    Unit IStatementVisitor<Unit>.Visit<TStatement>(TStatement statement)
    {
        Execute(statement);
        return new Unit();
    }


    public void Execute<TStatement>(TStatement statement) where TStatement : Statement
    {
        switch (statement)
        {
            case ExpressionStatement(var expression):
                Evaluate(expression);
                return;
            case Print(var expression):
                Console.WriteLine(Evaluate(expression).LoxPrint());
                return;
            case VarDeclaration(var name, var initializer):
            {
                object? value = null;
                if (initializer is not null) value = Evaluate(initializer);

                try
                {
                    Context.Define(name, value);
                }
                catch (ArgumentException)
                {
                    // TODO: Custom exception for variable redefinition
                    throw new LoxVariableUndefinedException($"Variable `{name}` is already defined.",
                        statement.Location);
                }

                return;
            }
            case Block(var statements):
            {
                ExecuteBlock(statements, new ExecutionContext(Context));
                return;
            }
            case If(var condition, var thenBranch, var elseBranch):
            {
                if (Evaluate(condition).ToLoxBool())
                    Execute(thenBranch);
                else if (elseBranch is not null)
                    Execute(elseBranch);
                return;
            }
            case While(var condition, var body):
            {
                while (Evaluate(condition).ToLoxBool())
                    try
                    {
                        Execute(body);
                    }
                    catch (LoxBreakException)
                    {
                        break;
                    }

                return;
            }
            case Break @break:
            {
                throw new LoxBreakException("Break should be only used inside loops.", @break.Location);
            }
            case Function(var name, _, _) s:
            {
                var function = new LoxFunction(s, Context, false);
                Context.Define(name, function);
                return;
            }
            case Return(var expression) ret:
            {
                object? value = null;
                if (expression is not null)
                    value = Evaluate(expression);
                throw new LoxReturnException(value,
                    "Return should only be used inside functions.", ret.Location);
            }
            case Class(var name, var body):
            {
                Context.Define(name, null);
                Dictionary<string, LoxFunction> methods = new();
                foreach (var method in body)
                {
                    var func = new LoxFunction(method, Context, method.Name == "init");
                    methods[method.Name] = func;
                }

                LoxClass @class = new LoxClass(name, methods);
                Context.Assign(name, @class);
                return;
            }
        }

        byte staticAssert = Statement.InheritorsAmount == 11 ? 0 : -1;
        _ = staticAssert;
        throw new UnreachableException("Not all cases are handled");
    }

    public void ExecuteBlock(IList<Statement> statements, ExecutionContext ctx)
    {
        var previous = Context;
        try
        {
            Context = ctx;
            foreach (var statement in statements)
                Execute(statement);
        }
        finally
        {
            Context = previous;
        }
    }

    public object? Evaluate<TExpression>(TExpression expression) where TExpression : Expression
    {
        switch (expression)
        {
            case Grouping(var inner): return Evaluate(inner);
            case Literal(var literal): return literal;
            case Unary(var expr, var op):
            {
                var right = Evaluate(expr);
                return op.Type switch
                {
                    TokenType.Minus => -right.ToLoxDouble(expr),
                    TokenType.Bang => !right.ToLoxBool(),
                    _ => throw new UnreachableException("This should be unreachable")
                };
            }
            case Binary(var left, var right) e when e is not LogicalAnd and not LogicalOr:
            {
                var leftValue = Evaluate(left);
                var rightValue = Evaluate(right);

                switch (e)
                {
                    case Addition:
                        return (leftValue, rightValue) switch
                        {
                            (string a, string b) => a + b,
                            (double a, double b) => a + b,
                            _ => throw new LoxCastException(
                                "Both operands of addition should be either numbers or strings.",
                                left.Location, e)
                        };
                    case Equality: return leftValue.LoxEquals(rightValue);
                    case Inequality: return !leftValue.LoxEquals(rightValue);
                }

                var leftNumber = leftValue.ToLoxDouble(left);
                var rightNumber = rightValue.ToLoxDouble(right);
                return e switch
                {
                    Subtraction => leftNumber - rightNumber,
                    Multiplication => leftNumber * rightNumber,
                    Division => leftNumber.LoxDivide(rightNumber, e.Location),
                    Greater => leftNumber > rightNumber,
                    GreaterEqual => leftNumber >= rightNumber,
                    Less => leftNumber < rightNumber,
                    LessEqual => leftNumber <= rightNumber,
                    var other =>
                        throw new
                            UnreachableException(
                                $"{other.GetType().Name}: This should be handled by previous switch case")
                };
            }
            case Binary(var left, var right) e when e is LogicalAnd or LogicalOr:
            {
                var leftValue = Evaluate(left);
                var leftBool = leftValue.ToLoxBool();
                if (e is LogicalOr)
                {
                    if (leftBool) return leftValue;
                }
                else if (e is LogicalAnd)
                {
                    if (!leftBool) return leftValue;
                }

                return Evaluate(right);
            }
            case Assign(var name, var expr) e:
            {
                var value = Evaluate(expr);
                bool found = Locals.TryGetValue(e, out var distanceFromScope);
                if (found)
                    Context.AssignAt(distanceFromScope, name, value);
                else
                    try
                    {
                        Globals.Assign(name, value);
                    }
                    catch (ArgumentException)
                    {
                        throw new LoxVariableUndefinedException($"Could not assign to undefined variable `{name}`.",
                            expr.Location);
                    }

                return value;
            }
            case Call(var calleeExpr, var argumentsExpr) callExpr:
            {
                var callee = Evaluate(calleeExpr);

                var arguments = argumentsExpr.Select(Evaluate).ToArray();
                var calleeLoc = calleeExpr.Location;
                if (callee is null) throw new LoxCastException("Could not call null.", calleeLoc, calleeExpr);
                if (callee is not ILoxCallable c)
                    throw new LoxCastException("Could not call non-callable.", calleeLoc, calleeExpr);
                if (arguments.Length != c.Arity)
                    throw new LoxRuntimeException($"Expected {c.Arity} arguments, but got {arguments.Length}.",
                        callExpr.Location);

                return c.Call(this, arguments);
            }
            case ReadVariable(var name) e:
            {
                try
                {
                    return LookupVariable(name, e);
                }
                catch (ArgumentException)
                {
                    throw new LoxVariableUndefinedException($"{name} is not defined.", e.Location);
                }
            }
            case This @this:
                return LookupVariable("this", @this);
            case Get(var obj, var name) e:
            {
                object? objValue = Evaluate(obj);
                if (objValue is LoxInstance loxInstance)
                    return loxInstance.Get(name);

                throw new LoxTypeException("Only instances have properties.", e.Location);
            }
            case Set(var obj, var name, var value) er:
            {
                object? receiver = Evaluate(obj);
                if (receiver is not LoxInstance loxInstance)
                    throw new LoxTypeException("Only instances have fields", er.Location);

                object? valObj = Evaluate(value);
                loxInstance.Set(name, valObj);
                return valObj;
            }

            case Lambda(_, _) s:
            {
                var function = new LoxFunction(s, Context, false);
                return function;
            }
        }

        byte staticAssert = Expression.InheritorsAmount == 25 ? 0 : -1;
        _ = staticAssert;
        // TODO: Evaluate sequence expressions
        throw new UnreachableException("Not all cases are handled for some reason");
    }

    private object? LookupVariable(string name, Expression expr)
    {
        bool present = Locals.TryGetValue(expr, out var distanceFromScope);
        if (present)
            return Context.GetAt(distanceFromScope, name);
        return Globals.Get(name);
    }

    public void Resolve(Expression expression, int scopesCount)
    {
        Locals[expression] = scopesCount;
    }
}