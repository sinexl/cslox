using System.Text;
using Expression = cslox.Ast.Generated.Expression;

namespace cslox;

public static class Util
{
    public static void Report(SourceLocation loc, string message)
    {
        Console.Error.WriteLine($"{loc}: {message}");
    }

    public static void Report(SourceLocation loc, string where, string message)
    {
        Console.Error.WriteLine($"{loc}: ({where}) {message}");
    }

    // returns true if no errors were reported. 
    public static bool ReportAllErrorsIfSome(IList<Error> errors)
    {
        if (errors.Count == 0) return true;
        foreach (var error in errors)
        {
            Console.Error.WriteLine(error);
        }

        return false;
    }

    public static void ReportException(LoxRuntimeException exception)
    {
        Console.Error.WriteLine($"{exception}");
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

    public static string ArrayTreePrint(this IEnumerable<Expression> array, int indent = 0)
    {
        var sb = new StringBuilder();
        var tab = new string(' ', indent * 2);
        foreach (var e in array)
        {
            var asStr = e.TreePrint(indent + 2);
            sb.Append($"{tab}{e.TreePrint(indent + 1)}");
        }

        return sb.ToString();
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

public struct Unit : IEquatable<Unit>
{
    public override string ToString() => "()";
    public bool Equals(Unit other) => true;
    public override bool Equals(object? obj) => obj is Unit;

    public static bool operator ==(Unit _, Unit __) => true;
    public static bool operator !=(Unit _, Unit __) => false;
    public override int GetHashCode() => 0;
}