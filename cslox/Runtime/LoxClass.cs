namespace cslox.Runtime;

public class LoxClass : ILoxCallable
{
    public LoxClass(Identifier name, Dictionary<string, LoxFunction> methods)
    {
        Methods = methods;
        Name = name;
    }

    public Identifier Name { get; init; }
    public Dictionary<string, LoxFunction> Methods { get; init; }

    public object Call(Interpreter interpreter, IList<object?> arguments)
    {
        LoxInstance instance = new(this);
        LoxFunction? initializer = GetMethod("init");
        if (initializer is not null)
            initializer.Bind(instance).Call(interpreter, arguments);

        return instance;
    }

    public int Arity
    {
        get
        {
            var initializer = GetMethod("init");
            if (initializer is null) return 0;
            return initializer.Arity;
        }
    }

    public override string ToString() => $"<class {Name.Id}>";

    public LoxFunction? GetMethod(string name) => Methods.GetValueOrDefault(name);
}

public class LoxInstance
{
    public LoxInstance(LoxClass @class)
    {
        Class = @class;
        Fields = new Dictionary<string, object?>();
    }

    public LoxClass Class { get; init; }
    public Dictionary<string, object?> Fields { get; init; }

    public override string ToString() => Class.Name.Id + " instance";

    public object? Get(Identifier name)
    {
        if (Fields.TryGetValue(name.Id, out var field))
            return field;

        LoxFunction? method = Class.GetMethod(name.Id);
        if (method is not null)
            return method.Bind(this);

        // TODO: Custom exception for this.
        throw new LoxVariableUndefinedException($"Undefined field `{name.Id}`.", name.Location);
    }

    public void Set(Identifier name, object? value)
    {
        Fields[name.Id] = value;
    }
}