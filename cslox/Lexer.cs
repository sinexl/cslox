using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices.Swift;

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
}

[DebuggerDisplay("{Src.Substring(_state.Current)}")]
public class Lexer
{
    private LexerState _state;


    public string FilePath;

    public Lexer(string src, string filePath)
    {
        Src = src;
        FilePath = filePath;
        _state = new LexerState();
    }

    public List<Error> Errors { get; } = new();
    public string Src { get; init; }

    public IEnumerable<Token> Accumulate()
    {
        var tokens = new List<Token>();
        while (!IsEof())
        {
            var token = ScanSingle();
            if (token is not null)
            {
                tokens.Add(token);
            }
            else
                continue;
        }

        tokens.Add(new Token(TokenType.Eof, "", null, SourceLoc));

        return tokens.ToArray();
    }

    public SourceLocation SourceLoc => new(FilePath, _state.LineNumber, _state.LineStart);

    [Pure]
    public Token? ScanSingle()
    {
        while (!IsEof())
        {
            var state = SaveState();
            char c = NextChar();
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
                case '!':
                    return CreateToken(Match('=') ? TokenType.BangEqual : TokenType.Bang);
                case '=':
                    return CreateToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal);
                case '>':
                    return CreateToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater);
                case '<':
                    return CreateToken(Match('=') ? TokenType.LessEqual : TokenType.Less);
                case '/':
                {
                    if (Match('/'))
                        while (PeekChar() != '\n' && !IsEof())
                            SkipChar();
                    else
                        return AddToken(TokenType.Slash);

                    break;
                }
                case ' ' or '\r' or '\t':
                    break;
                case '\n':
                {
                    _state.LineNumber++;
                    break;
                }
                case '"':
                {
                    RestoreState(state);
                    return ScanString();
                }

                case var d when char.IsDigit(d):
                {
                    RestoreState(state);
                    return ScanNumber();
                }

                case var ch when ch.IsIdBeginning():
                {
                    RestoreState(state);
                    return ScanIdentifier();
                }
                default:
                    Error($"unexpected character '{c}'"); break;
            }
        }

        return null;
    }

    [Pure]
    private Token? ScanIdentifier()
    {
        var start = _state.Current;
        while (PeekChar().IsId())
        {
            SkipChar();
        }

        var word = Src.AsSpan(start, _state.Current - start);

        // TODO: factor out this switch case. 
        TokenType type = word switch
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
            _ => TokenType.Identifier
        };

        string wordStr = word.ToString();
        return CreateToken(type, wordStr, wordStr);
        // return AddToken() 
    }

    [Pure]
    private Token? ScanNumber()
    {
        var start = _state.Current;
        while (char.IsDigit(PeekChar()))
        {
            SkipChar();
        }

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
    private Token CreateToken(TokenType type, string lexeme, object? literal = null)
    {
        return new Token(type, lexeme, literal, SourceLoc);
    }

    private Token CreateToken(TokenType type, object? literal = null)
    {
        return new Token(type, Src[_state.Current].ToString(), literal, SourceLoc);
    }

    [Pure]
    private char NextChar() => Src[_state.Current++];

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
        string src = """
                     // this is a comment
                     (( )){} // grouping stuff
                     !*+-/=<> <= == // operators
                     "String Literal" 123 hello and or _bob bob123 bob_
                     """;
        var self = new Lexer(src, "self");
        Token[] tokens = self.Accumulate().ToArray();
        foreach (var token in tokens)
        {
            Console.WriteLine(token);
        }
    }

    public LexerState SaveState() => _state;
    public void RestoreState(LexerState state) => _state = state;

    public bool IsEof(int index) => index >= Src.Length;
    public bool IsEof() => _state.Current >= Src.Length;

    private void Error(string message)
    {
        Errors.Add(new Error(SourceLoc, message));
    }

    private bool StartsWith(string prefix)
    {
        if (IsEof(_state.Current + prefix.Length)) return false;
        if (Src.AsSpan(_state.Current).StartsWith(prefix))
        {
            _state.Current += prefix.Length;
            return true;
        }

        return false;
    }
}

public static class Extensions
{
    public static bool HasLexeme(this TokenType type) => type switch
    {
        TokenType.Number or TokenType.String => true,
        _ => false
    };

    public static bool IsIdBeginning(this char c) => char.IsLetter(c) || c == '_';
    public static bool IsId(this char c) => char.IsLetter(c) || char.IsDigit(c) || c == '_';
}