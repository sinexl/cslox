namespace cslox.Runtime;

public class LoxClass : ILoxCallable
{
    public LoxClass(Identifier name)
    {
        Name = name;
    }
    public Identifier Name { get; init; }

    public override string ToString() => $"<class {Name.Id}>";

    public object? Call(Interpreter interpreter, IList<object?> arguments)
    {
        LoxInstance instance = new(this);
        return instance;
    }

    public int Arity
    {
        get => 0; // TODO: Constructor arity
    }
}

public class LoxInstance
{
    public LoxInstance(LoxClass @class)
    {
        Class = @class;
    }
    public LoxClass Class { get; init; }
    public override string ToString() => Class.Name.Id + " instance"; 
    
}