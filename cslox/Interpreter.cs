using System.Diagnostics;
using cslox.Ast.Generated;

namespace cslox;

public class Interpreter : IExpressionVisitor<object?>
{
    public object? Visit<TExpression>(TExpression expression) where TExpression : Expression
        => Evaluate(expression);

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
                    TokenType.Minus => right.ToLoxDouble(expr),
                    TokenType.Bang => !right.ToLoxBool(),
                    _ => throw new UnreachableException("This should be unreachable")
                };
            }
            case Binary(var left, var right) e:
            {
                object? leftValue = Evaluate(left);
                object? rightValue = Evaluate(right);

                if (e is Addition)
                {
                    return (leftValue, rightValue) switch
                    {
                        (string a, string b) => a + b,
                        (double a, double b) => a + b,
                        _ => throw new LoxCastException(
                            "Both operands of addition should be either numbers or strings.",
                            left.Location, e)
                    };
                }

                var leftNumber = leftValue.ToLoxDouble(left);
                var rightNumber = rightValue.ToLoxDouble(right);
                return e switch
                {
                    Addition => throw new UnreachableException("This should be handled by if."),
                    Subtraction => leftNumber - rightNumber,
                    Multiplication => leftNumber * rightNumber,
                    Division => leftNumber / rightNumber,
                    Greater => leftNumber > rightNumber,
                    GreaterEqual => leftNumber >= rightNumber,
                    Less => leftNumber < rightNumber,
                    LessEqual => leftNumber <= rightNumber,
                    Equality => leftValue.LoxEquals(rightValue),
                    Inequality => !leftValue.LoxEquals(rightValue),
                    _ => throw new UnreachableException("This should be unreachable")
                };
            }
        }

        byte staticAssert = Expression.InheritorsAmount == 16 ? 0 : -1;
        _ = staticAssert;
        throw new UnreachableException("Not all cases are handled for some reason");
    }
}

public static class InterpreterExtensions
{
    public const double Tolerance = 0.0001;

    public static bool ToLoxBool(this object? boolean) => boolean switch
    {
        null => false,
        bool b => b,
        _ => true
    };

    public static bool LoxEquals(this object? left, object? right) => (left, right) switch
    {
        (null, null) => true,
        (null, _) or (_, null) => false,
        (double a, double b) => Math.Abs(a - b) < Tolerance,
        var (a, b) => a == b,
    };

    public static double ToLoxDouble(this object? obj, Expression e)
    {
        if (obj is double) return Convert.ToDouble(obj);
        throw new LoxCastException($"Could not perform cast of `{obj}` to double", e.Location, e);
    }
}

public class LoxRuntimeException : Exception
{
    public SourceLocation Location { get; init; }

    // public LoxRuntimeException(SourceLocation location)
    //     : base($"{location}: Runtime error occurred.")
    // {
    //     Location = location;
    // }

    public LoxRuntimeException(string message, SourceLocation location)
        : base($"{location}: {message}")
    {
        Location = location;
    }

    public LoxRuntimeException(string message, SourceLocation location, Exception inner)
        : base($"{location}: {message}", inner)
    {
        Location = location;
    }

    public override string ToString() => $"{Location}: {Message}";

}

public class LoxCastException : LoxRuntimeException
{
    Expression? Expression { get; init; }

    public LoxCastException(string message, SourceLocation location, Expression? expression = null) : base(message,
        location)
    {
        Expression = expression;
    }

    public LoxCastException(string message, SourceLocation location, Exception inner, Expression? expression = null) :
        base(message, location, inner)
    {
        Expression = expression;
    }
}