using cslox.Ast.Generated;

namespace cslox.Runtime;

public interface ILoxCallable
{
    int Arity { get; }
    object? Call(Interpreter interpreter, IList<object?> arguments);
}

public class DotnetFunction : ILoxCallable
{
    public DotnetFunction(int arity, Func<object?> callback)
    {
        Callback = callback;
        Arity = arity;
    }

    public Func<object?> Callback { get; }

    public object? Call(Interpreter interpreter, IList<object?> arguments) => Callback();

    public int Arity { get; }
}

public class LoxFunction : ILoxCallable
{
    // Full version of constructor
    public LoxFunction(IList<Identifier> parameters, IList<Statement> body, Identifier? name, int arity,
        SourceLocation location, ExecutionContext closure, bool isInitializer)
    {
        Closure = closure;
        IsInitializer = isInitializer;
        Location = location;
        Params = parameters.ToArray();
        Arity = arity;
        Name = name;
        Body = body.ToArray();
    }

    // Constructor for Ast.Generated.Function  
    public LoxFunction(Function declaration, ExecutionContext closure, bool isInitializer) :
        this(declaration.Params, declaration.Body, declaration.Name, declaration.Params.Length, declaration.Location,
            closure, isInitializer)
    { }

    public LoxFunction(Lambda lambda, ExecutionContext closure, bool isInitializer) :
        this(lambda.Params, lambda.Body, null, lambda.Params.Length, lambda.Location, closure, isInitializer)
    { }

    public bool IsInitializer { get; init; }

    public Identifier[] Params { get; }
    public Statement[] Body { get; }
    public Identifier? Name { get; }
    public SourceLocation Location { get; }

    public ExecutionContext Closure { get; }


    public object? Call(Interpreter interpreter, IList<object?> arguments)
    {
        var context = new ExecutionContext(Closure);
        for (int i = 0; i < Params.Length; i++) context.Define(Params[i].Id, arguments[i]);

        try
        {
            interpreter.ExecuteBlock(Body, context);
        }
        catch (LoxReturnException e)
        {
            if (IsInitializer) return Closure.GetAt(0, "this");
            return e.Value;
        }

        if (IsInitializer) return Closure.GetAt(0, "this");

        return null;
    }

    public int Arity { get; }

    public override string ToString() => Name is not null ? $"<fun {Name}>" : "<anonymous fun>";

    public LoxFunction Bind(LoxInstance instance)
    {
        var context = new ExecutionContext(Closure);
        context.Define("this", instance);
        return new LoxFunction(Params, Body, Name, Arity, Location, context, IsInitializer);
    }
}