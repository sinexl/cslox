using System.Diagnostics;
using cslox.Ast.Generated;

namespace cslox.Runtime;

public class Interpreter : IExpressionVisitor<object?>, IStatementVisitor<Unit>
{
    public Interpreter()
    {
        _locals = new(); 
        Globals = new ExecutionContext();
        Context = Globals;
        Globals.Define("clock", new DotnetFunction(0, () => DateTimeOffset.Now.ToUnixTimeMilliseconds() / 1000.0));
    }

    public ExecutionContext Globals { get; }
    public ExecutionContext Context { get; set; }

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
                var function = new LoxFunction(s, Context);
                Context.Define(name, function);
                return;
            }
            case Return(var expression) ret:
            {
                var value = Evaluate(expression);
                throw new LoxReturnException(value,
                    "Return should only be used inside functions.", ret.Location);
            }
        }

        byte staticAssert = Statement.InheritorsAmount == 10 ? 0 : -1;
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
            case Assign(var name, var expr):
            {
                var value = Evaluate(expr);
                try
                {
                    Context.Assign(name, value);
                    return value;
                }
                catch (ArgumentException)
                {
                    throw new LoxVariableUndefinedException($"Could not assign to undefined variable `{name}`.",
                        expr.Location);
                }
            }
            case Call(var calleeExpr, var argumentsExpr):
            {
                var callee = Evaluate(calleeExpr);

                var arguments = argumentsExpr.Select(Evaluate).ToArray();
                var loc = calleeExpr.Location;
                if (callee is null) throw new LoxCastException("Could not call null.", loc, calleeExpr);
                if (callee is not ILoxCallable c)
                    throw new LoxCastException("Could not call non-callable.", loc, calleeExpr);
                if (arguments.Length != c.Arity)
                    throw new LoxRuntimeException($"Expected {c.Arity} arguments, but got {arguments.Length}.",
                        loc);

                return c.Call(this, arguments);
            }
            case ReadVariable(var name) e:
            {
                try
                {
                    return Context.Get(name);
                }
                catch (ArgumentException)
                {
                    throw new LoxVariableUndefinedException($"{name} is not defined.", e.Location);
                }
            }
            case Lambda(_, _) s:
            {
                var function = new LoxFunction(s, Context);
                return function;
            }
        }

        byte staticAssert = Expression.InheritorsAmount == 22 ? 0 : -1;
        _ = staticAssert;
        // TODO: Evaluate sequence expressions
        throw new UnreachableException("Not all cases are handled for some reason");
    }

    public void Resolve(Expression expression, int scopesCount)
    {
        _locals[expression] = scopesCount; 
    }

    private Dictionary<Expression, int> _locals; 
}