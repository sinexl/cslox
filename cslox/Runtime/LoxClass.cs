namespace cslox.Runtime;

public class LoxClass : ILoxCallable
{
    public LoxClass(Identifier name, LoxClass? superclass, Dictionary<string, LoxFunction> methods)
    {
        Methods = methods;
        Name = name;
        Superclass = superclass;
    }

    public Identifier Name { get; init; }
    public Dictionary<string, LoxFunction> Methods { get; init; }
    public LoxClass? Superclass { get; init; }

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

    public LoxFunction? GetMethod(string name)
    {
        if (Methods.TryGetValue(name, out var method))
            return method;
        if (Superclass is not null)
            return Superclass.GetMethod(name);
        return null;
    }
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

        throw new LoxUndefinedFieldException($"Undefined field `{name.Id}`.", name.Location);
    }

    public void Set(Identifier name, object? value)
    {
        Fields[name.Id] = value;
    }
}