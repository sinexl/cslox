namespace cslox;

public class Util
{
    public static void Report(SourceLocation loc, string? where, string message)
    {
        if (!string.IsNullOrEmpty(where))
        {
            Console.Error.WriteLine($"{loc}: ({where}) {message}");
        }
        else
        {
            Console.Error.WriteLine($"{loc}: {message}");
        }
    }
}