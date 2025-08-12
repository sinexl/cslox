using System.Diagnostics;
using cslox.Ast.Generated;

namespace cslox.Runtime;

public class Interpreter : IExpressionVisitor<object?>, IStatementVisitor<Unit>
{
    public ExecutionContext Context { get; init; }

    public Interpreter()
    {
        Context = new();
    }

    object? IExpressionVisitor<object?>.Visit<TExpression>(TExpression expression)
        => Evaluate(expression);

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
                break;
            case Print(var expression):
                Console.WriteLine(Evaluate(expression).LoxPrint());
                break;
            case VarDeclaration(var name, var initializer):
            {
                object? value = null;
                if (initializer is not null)
                {
                    value = Evaluate(initializer);
                }

                Context.Define(name, value);
                break;
            }
            default:
                throw new UnreachableException("Not all cases are handled");
        }

        byte staticAssert = Statement.InheritorsAmount == 4 ? 0 : -1;
        _ = staticAssert;
    }

    public object? Evaluate<TExpression>(TExpression expression) where TExpression : Expression
    {
        switch (expression)
        {
            case Grouping(var inner): return Evaluate(inner);
            case Literal(var literal): return literal;
            case Unary(var expr, var op):
            {
                object? right = Evaluate(expr);
                return op.Type switch
                {
                    TokenType.Minus => -right.ToLoxDouble(expr),
                    TokenType.Bang => !right.ToLoxBool(),
                    _ => throw new UnreachableException("This should be unreachable")
                };
            }
            case Binary(var left, var right) e:
            {
                object? leftValue = Evaluate(left);
                object? rightValue = Evaluate(right);

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
                break;
            }
        }

        byte staticAssert = Expression.InheritorsAmount == 17 ? 0 : -1;
        _ = staticAssert;
        throw new UnreachableException("Not all cases are handled for some reason");
    }
}