using cslox.Ast.Generated;

namespace cslox.Runtime;

public interface ILoxCallable
{
    object? Call(Interpreter interpreter, IList<object?> arguments);
    int Arity { get; }
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

public class LoxFunction : ILoxCallable
{
    public LoxFunction(Function declaration)
    {
        Declaration = declaration;
    }

    public object? Call(Interpreter interpreter, IList<object?> arguments)
    {
        var context = new ExecutionContext(interpreter.Globals);
        for (int i = 0; i < Declaration.Params.Length; i++)
        {
            context.Define(Declaration.Params[i].Lexeme, arguments[i]);
        }

        try
        {
            interpreter.ExecuteBlock(Declaration.Body, context);
        }
        catch (LoxReturnException e)
        {
            return e.Value; 
        }
        return null; 
    }

    public Function Declaration { get; init; }

    public int Arity => Declaration.Params.Length;
    public override string ToString() => $"<fun {Declaration.Name}>"; 
}