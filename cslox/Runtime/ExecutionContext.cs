namespace cslox.Runtime;

public class ExecutionContext
{
    public ExecutionContext(ExecutionContext ctx)
    {
        Enclosing = ctx;
    }

    public ExecutionContext()
    {
        Enclosing = null;
    }

    public Dictionary<string, object?> Values { get; init; } = new();

    // Parent of the current context. 
    public ExecutionContext? Enclosing { get; set; }

    // Useful for REPLs 
    public bool AllowRedefinition { get; set; } = false;

    // @DFOI: Unlike in a book, it's not allowed to define variable with the same name twice.
    // (Unless AllowRedefinition is true) 
    public void Define(string name, object? value)
    {
        if (!AllowRedefinition)
        {
            if (!Values.TryAdd(name, value)) throw new ArgumentException($"{name} is already defined");
        }
        else
        {
            Values[name] = value;
        }
    }

    public object? Get(string name)
    {
        if (Values.TryGetValue(name, out var value))
            return value;
        if (Enclosing is not null)
            return Enclosing.Get(name);
        throw new ArgumentException($"{name} is not defined.");
    }

    public object? GetAt(int distance, string name) => GetAncestor(distance).Values[name];

    public ExecutionContext GetAncestor(int distance)
    {
        var ctx = this;
        for (int i = 0; i < distance; i++)
            ctx = ctx.Enclosing ?? throw new ArgumentException($"Ancestor at {distance} is not defined.");

        return ctx;
    }

    public void Clear()
    {
        Values.Clear();
    }

    public void Assign(string name, object? value)
    {
        if (Values.ContainsKey(name))
        {
            Values[name] = value;
            return;
        }

        if (Enclosing is not null)
        {
            Enclosing.Assign(name, value);
            return;
        }

        throw new ArgumentException($"{name} is not defined.");
    }

    public void AssignAt(int distance, string name, object? value)
    {
        GetAncestor(distance).Values[name] = value;
    }
}