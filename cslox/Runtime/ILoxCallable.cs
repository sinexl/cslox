namespace cslox.Runtime;

public interface ILoxCallable
{
    object? Call(Interpreter interpreter, IList<object?> arguments );
    int Arity { get; init; }
}

public class DotnetFunction : ILoxCallable
{
    public DotnetFunction(int arity, Func<object?> callback)
    {
        Callback = callback;
        Arity = arity;
    }

    public object? Call(Interpreter interpreter, IList<object?> arguments)
    {
        return Callback(); 
    }

    public Func<object?> Callback { get; init; }  
    public int Arity { get; init; }
} 