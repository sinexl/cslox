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

    Eof
}