using System.Threading.Tasks.Sources;

namespace cslox.Runtime;

public class ExecutionContext
{
    public Dictionary<string, object?> Values { get; init; } = new();

    // Useful for REPLs 
    public bool AllowRedefinition { get; set; } = false;

    // @DFOI: Unlike in a book, it's not allowed to define variable with the same name twice.
    // (Unless AllowRedefinition is true) 
    public void Define(string name, object? value)
    {
        if (!AllowRedefinition)
        {
            //                               TODO: Throw custom exception.
            if (!Values.TryAdd(name, value)) throw new ArgumentException($"{name} is already defined");
        }
        else
            Values[name] = value;
    }

    public object? Get(string name)
    {
        if (Values.TryGetValue(name, out var value))
            return value;
        throw new ArgumentException($"{name} is not defined.");
    }

    public void Clear()
    {
        Values.Clear();
    }

    public void Assign(string name, object? value)
    {
        if (Values.ContainsKey(name)) Values[name] = value;
        else throw new ArgumentException($"{name} is not defined.");
    }
}