using System.Diagnostics.Contracts;
using System.Reflection.Metadata;
using cslox;

if (args.Length > 1)
{
    Console.WriteLine("Usage: cslox <file>. Currently only single file is supported");
    return -1;
}

if (args.Length == 1)
{
    RunFile(args[0]);
}
else
{
    Prompt();
}

return 0;

[Pure]
Error[] RunCode(string src, string? filePath = null)
{
    Console.WriteLine($"src: {src}");
    var lexer = new Lexer(src, filePath: filePath ?? "<REPL>");
    var tokens = lexer.Accumulate();
    if (lexer.Errors.Count > 0) return lexer.Errors.ToArray();


    foreach (Token token in tokens)
    {
    }

    return [];
}

void RunFile(string filePath)
{
    var src = File.ReadAllText(filePath);
    var errors = RunCode(src, filePath);
    var exit = ReportAllErrorsIfSome(errors);
    if (exit)
    {
        return;
    }
}

void Prompt()
{
    Console.WriteLine("NOTE: Enter :quit or Press Ctrl+D to quit.");
    while (true)
    {
        Console.Write("> ");
        string? line = Console.ReadLine();
        if (line is null || line == ":quit")
            break;
        var errors = RunCode(line);
        var exit = ReportAllErrorsIfSome(errors);
        Console.WriteLine($"Found {errors.Length} errors");
    }
}


bool ReportAllErrorsIfSome(Error[] errors1)
{
    throw new NotImplementedException();
}

public record struct SourceLocation()
{
    public string File { get; set; } 
    public int Line { get; set; }
    public int Offset   { get; set; }

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