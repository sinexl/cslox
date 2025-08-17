using cslox.Ast.Generated;

namespace cslox.Runtime;

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

public class LoxVariableUndefinedException : LoxRuntimeException
{
    public LoxVariableUndefinedException(string message, SourceLocation location) : base(message, location)
    {
    }
}

public class LoxBreakException : LoxRuntimeException
{
    public LoxBreakException(string msg, SourceLocation location) : base(msg, location) 
    {
    }
}

// if this exception wasn't caught, `return` was used not in function; 
public class LoxReturnException : LoxRuntimeException
{
    public LoxReturnException(object? value, string message, SourceLocation location) : base(message, location)
    {
        Value = value;
    }

    public object? Value { get; init; } 
}