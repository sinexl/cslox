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

    public Func<object?> Callback { get; }
    public int Arity { get; }
}

public class LoxFunction : ILoxCallable
{
    // Full version of constructor
    public LoxFunction(IList<Identifier> parameters, IList<Statement> body, Identifier? name, int arity,
        SourceLocation location, ExecutionContext closure)
    {
        Closure = closure;
        Location = location;
        Params = parameters.ToArray();
        _arity = arity;
        Name = name;
        Body = body.ToArray();
    }

    // Constructor for Ast.Generated.Function  
    public LoxFunction(Function declaration, ExecutionContext closure) : 
        this(declaration.Params, declaration.Body, declaration.Name, declaration.Params.Length, declaration.Location, closure)
    {
    }

    public LoxFunction(Lambda lambda, ExecutionContext closure) :
        this(lambda.Params, lambda.Body, null, lambda.Params.Length, lambda.Location, closure)
    {
        
    }


    public object? Call(Interpreter interpreter, IList<object?> arguments)
    {
        var context = new ExecutionContext(Closure);
        for (int i = 0; i < Params.Length; i++)
        {
            context.Define(Params[i].Id, arguments[i]);
        }

        try
        {
            interpreter.ExecuteBlock(Body, context);
        }
        catch (LoxReturnException e)
        {
            return e.Value;
        }

        return null;
    }

    public Identifier[] Params { get; }
    public Statement[] Body { get; }
    private int _arity;
    public int Arity => _arity;
    public Identifier? Name { get; }
    public SourceLocation Location { get; }

    public ExecutionContext Closure { get; }
    public override string ToString() => Name is not null ? $"<fun {Name}>" : "<anonymous fun>";
}