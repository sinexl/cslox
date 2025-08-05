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
public static class Extensions
{
    public static bool HasLexeme(this TokenType type) => type switch
    {
        TokenType.Number or TokenType.String => true,
        _ => false
    };

    public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
    {
        foreach (var x in enumerable)
            action(x);
    }

    public static bool IsIdBeginning(this char c) => char.IsLetter(c) || c == '_';
    public static bool IsId(this char c) => char.IsLetter(c) || char.IsDigit(c) || c == '_';
}
public record struct SourceLocation()
{
    public string File { get; set; }
    public int Line { get; set; }
    public int Offset { get; set; }

    public SourceLocation(string file, int line, int offset) : this()
    {
        File = file;
        Line = line;
        Offset = offset;
    }

    public override string ToString() => $"{File}:{Line}:{Offset}";
}

public record class Error(SourceLocation Location, string Message)
{
    public override string ToString() => $"{Location}: {Message}";
}
