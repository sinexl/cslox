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

public record struct SourceLocation(string File, int Line, int Offset)
{
    public override string ToString() => $"{File}:{Line}:{Offset}";
}

public record class Error(SourceLocation Location, string Message)
{
    public override string ToString() => $"{Location}: {Message}"; 
}

public class Token
{
    public Token()
    {
    }

    public Token(TokenType type, string lexeme, object? literal, SourceLocation loc)
    {
        Type = type;
        Lexeme = lexeme;
        Literal = literal;
        Location = loc; 
    }

    public TokenType Type { get; set; } 
    public SourceLocation Location { get; set; }
    public Object? Literal { get; set; } 
    public string Lexeme  { get; set; } = "";
    public override string ToString() => $"{Type}: {Literal ?? "<nil>"}, Lexeme: {(!string.IsNullOrEmpty(Lexeme) ? Lexeme : "\"\"")}"; 
}

public enum TokenType {
    // Single-character tokens.
    LeftParen, RightParen, LeftBrace, RightBrace,
    Comma, Dot, Minus, Plus, Semicolon, Slash, Star,

    // One or two character tokens.
    Bang, BangEqual,
    Equal, EqualEqual,
    Greater, GreaterEqual,
    Less, LessEqual,

    // Literals.
    Identifier, String, Number,

    // Keywords.
    And, Class, Else, False, Fun, For, If, Nil, Or,
    Print, Return, Super, This, True, Var, While,

    Eof
}
