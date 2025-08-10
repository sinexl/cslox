using System.Diagnostics;
using System.Globalization;
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

    public static string LoxPrint(this object? obj)
    {
        return obj switch
        {
            null => "nil",
            bool b => b ? "true" : "false", // for some reason bool.ToString() returns Capitalized value (True, False) 
            string s => s,
            //                             G in this case removes .0
            double d => d.ToString("G", CultureInfo.InvariantCulture),
            _ => obj.ToString() ?? throw new UnreachableException("This should be unreachable")
        };
    }

    public static double LoxDivide(this double left, double right, SourceLocation loc)
    {
        if (right == 0.0)
            throw new LoxZeroDivideException("Zero division is not allowed.", loc);
        return left / right; 
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

public class LoxZeroDivideException : LoxRuntimeException
{
    public LoxZeroDivideException(string message, SourceLocation location) : base(message, location)
    {
    }

    public LoxZeroDivideException(string message, SourceLocation location, Exception inner) : base(message, location,
        inner)
    {
    }
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