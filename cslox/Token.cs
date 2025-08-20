namespace cslox;

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
    public string Lexeme { get; set; } = "";

    public override string ToString()
    {
        string lexeme = !string.IsNullOrEmpty(Lexeme) && Type.HasLexeme() ? $"Lexeme: {Lexeme}" : "";
        string literal = Literal is not null ? $" ({Literal})" : "";
        return $"{Location}: {Type}{literal}, {lexeme}";
    }

    public bool Expect(TokenType expected)
    {
        if (Type == expected) return true;
        Util.Report(Location, $"Expected {expected.Humanize()}, but got {Type.Humanize()}");
        return false;
    }

    public Identifier ToIdentifier()
    {
        if (Type is not TokenType.Identifier) throw new ArgumentException("Token is not an identifier");
        return new Identifier(Lexeme, Location);
    }
}

public enum TokenType
{
    // Single-character tokens.
    LeftParen,
    RightParen,
    LeftBrace,
    RightBrace,
    Comma,
    Dot,
    Minus,
    Plus,
    Semicolon,
    Slash,
    Star,

    // One or two character tokens.
    Bang,
    BangEqual,
    Equal,
    EqualEqual,
    Greater,
    GreaterEqual,
    Less,
    LessEqual,

    // Literals.
    Identifier,
    String,
    Number,

    // Keywords.
    And,
    Class,
    Else,
    False,
    Fun,
    For,
    If,
    Nil,
    Or,
    Print,
    Return,
    Super,
    This,
    True,
    Var,
    While,
    Break,

    Eof
}

public static class TokenTypeExtensions
{
    public static bool IsStatementBeginning(this TokenType type) =>
        type switch
        {
            TokenType.Class or TokenType.Fun or TokenType.Var or TokenType.For or TokenType.If or TokenType.While
                or TokenType.Print or TokenType.Return => true,
            _ => false
        };

    public static string? Terminal(this TokenType type) =>
        type switch
        {
            TokenType.LeftParen => "(",
            TokenType.RightParen => ")",
            TokenType.LeftBrace => "{",
            TokenType.RightBrace => "}",
            TokenType.Comma => ",",
            TokenType.Dot => ".",
            TokenType.Minus => "-",
            TokenType.Plus => "+",
            TokenType.Semicolon => ";",
            TokenType.Slash => "/",
            TokenType.Star => "*",
            TokenType.Bang => "!",
            TokenType.BangEqual => "!=",
            TokenType.Equal => "=",
            TokenType.EqualEqual => "==",
            TokenType.Greater => ">",
            TokenType.GreaterEqual => ">=",
            TokenType.Less => "<",
            TokenType.LessEqual => "<=",
            TokenType.And => "and",
            TokenType.Class => "class",
            TokenType.Else => "else",
            TokenType.False => "false",
            TokenType.Fun => "fun",
            TokenType.For => "for",
            TokenType.If => "if",
            TokenType.Nil => "nil",
            TokenType.Or => "or",
            TokenType.Print => "print",
            TokenType.Return => "return",
            TokenType.Super => "super",
            TokenType.This => "this",
            TokenType.True => "true",
            TokenType.Var => "var",
            TokenType.While => "while",
            TokenType.Break => "break",
            TokenType.Identifier or TokenType.String or TokenType.Number or TokenType.Eof => null,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    public static string Humanize(this TokenType type) =>
        type switch
        {
            TokenType.Eof => "end of file",
            TokenType.LeftParen => "opening parenthesis",
            TokenType.RightParen => "closing parenthesis",
            TokenType.LeftBrace => "opening brace",
            TokenType.RightBrace => "closing brace",
            TokenType.Comma => "`,`",
            TokenType.Dot => "`.`",
            TokenType.Minus => "`-`",
            TokenType.Plus => "`+`",
            TokenType.Semicolon => "`;`",
            TokenType.Slash => "`/`",
            TokenType.Star => "`*`",
            TokenType.Bang => "`!`",
            TokenType.BangEqual => "`!=`",
            TokenType.Equal => "`=`",
            TokenType.EqualEqual => "`==`",
            TokenType.Greater => "`>`",
            TokenType.GreaterEqual => "`>=`",
            TokenType.Less => "`<`",
            TokenType.LessEqual => "`<=`",
            TokenType.Identifier => "identifier",
            TokenType.String => "string literal",
            TokenType.Number => "number literal",
            TokenType.Nil => "`nil`",
            TokenType.False => "`false`",
            TokenType.True => "`true`",
            // Keywords
            TokenType.And or TokenType.Class or TokenType.Else or TokenType.Fun or TokenType.For or TokenType.If or
                TokenType.Or or TokenType.Print or TokenType.Return or TokenType.Super or TokenType.This
                or TokenType.Var or TokenType.Break or
                TokenType.While => $"`{type.ToString().ToLower()}` keyword",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    public static TokenType ToTokenType(this ReadOnlySpan<char> s) => s switch
    {
        "and" => TokenType.And,
        "class" => TokenType.Class,
        "else" => TokenType.Else,
        "false" => TokenType.False,
        "fun" => TokenType.Fun,
        "for" => TokenType.For,
        "if" => TokenType.If,
        "nil" => TokenType.Nil,
        "or" => TokenType.Or,
        "print" => TokenType.Print,
        "return" => TokenType.Return,
        "super" => TokenType.Super,
        "this" => TokenType.This,
        "true" => TokenType.True,
        "var" => TokenType.Var,
        "while" => TokenType.While,
        "break" => TokenType.Break,
        _ => TokenType.Identifier
    };
}