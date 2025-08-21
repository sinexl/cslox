using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace cslox;

public record struct LexerState
{
    public LexerState()
    {
        Current = 0;
        LineStart = 0;
        LineNumber = 1;
    }

    public int Current { get; set; }
    public int LineStart { get; set; }
    public int LineNumber { get; set; }

    public SourceLocation ToSourceLocation(string file)
    {
        int offset = Current - LineStart;
        if (LineNumber > 1) offset -= 1;

        return new SourceLocation(file, LineNumber, offset);
    }
}

[DebuggerDisplay("{Src.Substring(_state.Current)}")]
public class Lexer
{
    private LexerState _state;
    private LexerState _tokenStart;

    public Lexer(string src, string filePath)
    {
        Src = src;
        FilePath = filePath;
        _state = new LexerState();
        _tokenStart = _state;
    }


    public string FilePath { get; init; }

    public List<Error> Errors { get; } = new();
    public string Src { get; init; }

    public SourceLocation SourceLoc => new(FilePath, _state.LineNumber, _state.Current - _state.LineStart);

    public static Lexer FromFile(string filePath) => new(File.ReadAllText(filePath), filePath);

    public Token[] Accumulate()
    {
        var tokens = new List<Token>();
        while (!IsEof())
        {
            var token = ScanSingle();
            if (token is not null)
                tokens.Add(token);
            else
                continue;
        }

        tokens.Add(new Token(TokenType.Eof, "", null, SourceLoc));

        return tokens.ToArray();
    }

    [Pure]
    public Token? ScanSingle()
    {
        SkipWhitespacesAndComments();
        if (IsEof()) return null;
        var saved = SaveState();
        char c = NextChar();
        _tokenStart = _state;
        switch (c)
        {
            case '(': return CreateToken(TokenType.LeftParen);
            case ')': return CreateToken(TokenType.RightParen);
            case '{': return CreateToken(TokenType.LeftBrace);
            case '}': return CreateToken(TokenType.RightBrace);
            case ',': return CreateToken(TokenType.Comma);
            case '.': return CreateToken(TokenType.Dot);
            case '-': return CreateToken(TokenType.Minus);
            case '+': return CreateToken(TokenType.Plus);
            case '*': return CreateToken(TokenType.Star);
            case ';': return CreateToken(TokenType.Semicolon);
            case '/':
            {
                var next = PeekNext();
                Debug.Assert(next is not '*' and not '/',
                    $"Comments should be eliminated by {nameof(SkipWhitespacesAndComments)}");
                return CreateToken(TokenType.Slash);
            }
            case '!':
                return CreateToken(Match('=') ? TokenType.BangEqual : TokenType.Bang);
            case '=':
                return CreateToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal);
            case '>':
                return CreateToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater);
            case '<':
                return CreateToken(Match('=') ? TokenType.LessEqual : TokenType.Less);
            // @whitespaces
            case ' ' or '\r' or '\t' or '\n':
            {
                throw new UnreachableException(
                    $"Whitespaces should be eliminated by {nameof(SkipWhitespacesAndComments)}");
            }
            case '"':
            {
                RestoreState(saved);
                return ScanString();
            }

            case var d when char.IsDigit(d):
            {
                RestoreState(saved);
                return ScanNumber();
            }

            case var ch when ch.IsIdBeginning():
            {
                RestoreState(saved);
                return ScanIdentifier();
            }
            default:
            {
                Error($"unexpected character '{c}'");
                break;
            }
        }

        return null;
    }

    private void SkipWhitespacesAndComments()
    {
        while (true)
            if (char.IsWhiteSpace(PeekChar()))
            {
                while (char.IsWhiteSpace(PeekChar()) && !IsEof())
                    SkipChar();
            }
            else if (StartsWith("/*"))
            {
                int nestedComments = 1;
                while (nestedComments > 0)
                {
                    while (!StartsWith("*/"))
                    {
                        if (StartsWithPeek("/*"))
                            nestedComments++;

                        if (IsEof())
                        {
                            Error("Unterminated multi-line comment");
                            return;
                        }

                        SkipChar();
                    }

                    nestedComments--;
                }
            }
            else if (StartsWith("//"))
            {
                while (PeekChar() != '\n' && !IsEof())
                    SkipChar();

                if (!IsEof()) SkipChar();
            }
            else
            {
                break;
            }
    }

    [Pure]
    private Token ScanIdentifier()
    {
        var start = _state.Current;
        while (PeekChar().IsId()) SkipChar();

        var word = Src.AsSpan(start, _state.Current - start);

        TokenType type = word.ToTokenType();

        string wordStr = word.ToString();
        return CreateToken(type, wordStr, wordStr);
        // return AddToken() 
    }

    [Pure]
    private Token? ScanNumber()
    {
        var start = _state.Current;
        while (char.IsDigit(PeekChar())) SkipChar();

        if (PeekChar() == '.' && char.IsDigit(PeekNext()))
        {
            SkipChar();
            var afterDot = PeekChar();
            if (!char.IsDigit(afterDot))
            {
                Error($"Expected decimal part of a number after '.', got: {afterDot}");
                return null;
            }

            while (char.IsDigit(PeekChar()))
                SkipChar();
        }

        string text = Src.Substring(start, _state.Current - start);

        return CreateToken(TokenType.Number, text, double.Parse(text));
    }


    [Pure]
    private Token? ScanString()
    {
        var length = 0;
        Debug.Assert(PeekChar() == '"');
        SkipChar();
        var start = _state.Current;
        while (PeekChar() != '"' && !IsEof())
        {
            // todo: escape sequences 
            if (PeekChar() == '\n') _state.LineNumber++;
            length++;
            SkipChar();
        }

        if (IsEof())
        {
            Error("Unterminated string literal");
            return null;
        }

        SkipChar(); // " 
        string value = Src.Substring(start, length);
        return CreateToken(TokenType.String, value, value);
    }

    private bool Match(char expected)

    {
        if (IsEof()) return false;
        char actual = PeekChar();
        if (actual != expected)
            return false;

        _state.Current++;
        return true;
    }

    [Pure]
    private Token CreateToken(TokenType type, string lexeme, object? literal = null) =>
        new(type, lexeme, literal, _tokenStart.ToSourceLocation(FilePath));

    private Token CreateToken(TokenType type)
    {
        var lexeme = type.Terminal();
        if (lexeme is null)
            throw new ArgumentException(
                "Use other overload of CreateToken for Tokens that have more than 1 possible lexeme");
        return new Token(type, lexeme, null, _tokenStart.ToSourceLocation(FilePath));
    }

    [Pure]
    private char NextChar()
    {
        char character = Src[_state.Current];
        switch (character)
        {
            case '\n':
            {
                _state.LineNumber++;
                _state.LineStart = _state.Current;
                break;
            }
        }

        _state.Current++;
        return character;
    }

    private void SkipChar() => _ = NextChar();

    private char PeekChar()
    {
        if (IsEof()) return '\0';
        return Src[_state.Current];
    }

    private char PeekNext()
    {
        var stateCurrent = _state.Current + 1;
        if (IsEof(stateCurrent)) return '\0';
        return Src[stateCurrent];
    }

    public static void Test()
    {
        string path = "./QuickTests/lexer.cslox";
        string src = File.ReadAllText(path);

        var self = new Lexer(src, path);
        Token[] tokens = self.Accumulate().ToArray();
        foreach (var token in tokens) Console.WriteLine(token);

        foreach (var error in self.Errors) Console.WriteLine($"Error: {error}");
    }

    public LexerState SaveState() => _state;

    public void RestoreState(LexerState state) => _state = state;

    private bool IsEof(int index) => index >= Src.Length;

    public bool IsEof() => _state.Current >= Src.Length;

    private void Error(string message)
    {
        Errors.Add(new Error(SourceLoc, message));
    }

    private bool StartsWith(string prefix)
    {
        bool res = StartsWithPeek(prefix);
        if (res) _state.Current += prefix.Length;
        return res;
    }

    private bool StartsWithPeek(string prefix)
    {
        if (IsEof(_state.Current + prefix.Length)) return false;
        if (Src.AsSpan(_state.Current).StartsWith(prefix))
            return true;

        return false;
    }
}