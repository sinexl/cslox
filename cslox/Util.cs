namespace cslox;

public class Util
{
    public static void Report(SourceLocation loc, string message)
    {
        Console.Error.WriteLine($"{loc}: {message}");
    }

    public static void Report(SourceLocation loc, string where, string message)
    {
        Console.Error.WriteLine($"{loc}: ({where}) {message}");
    }
}