using System.Diagnostics;
using System.Globalization;
using cslox.Ast.Generated;

namespace cslox.Runtime;

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