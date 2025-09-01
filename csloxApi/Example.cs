namespace csloxApi;

public class Example
{
    public static void Main()
    {
        var cslox = new Cslox(allowRedefinition: true);
        object? result = cslox.Evaluate("(2 + 3) * 4");
        Console.WriteLine(result); // 20 

        cslox.SetGlobalDouble("PI", Double.Pi);
        result = cslox.Evaluate("PI / 2");
        Console.WriteLine(result); // 1.57....
    }
}