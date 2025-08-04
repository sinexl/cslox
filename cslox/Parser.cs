using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;
using cslox.Ast;
using cslox.Ast.Generated;

namespace cslox;

/*
Syntax:
        expression     → equality ;
        equality       → comparison ( ( "!=" | "==" ) comparison )* ;
        comparison     → term ( ( ">" | ">=" | "<" | "<=" ) term )* ;
        term           → factor ( ( "-" | "+" ) factor )* ;
        factor         → unary ( ( "/" | "*" ) unary )* ;
        unary          → ( "!" | "-" ) unary
                       | primary ;
        primary        → NUMBER | STRING | "true" | "false" | "nil"
                       | "(" expression ")" ;

Precedence & Associativity  (from the lowest precedence to highest)
        Name          Operators      Associates
        ------------------------------------
        Equality       == !=         |  Left
        Comparison     > >= < <=     |  Left
        Term           - +           |  Left
        Factor         / *           |  Left
        Unary          ! -           |  Right
*/

public class Parser
{
    private readonly List<Token> _tokens;

    public struct State
    {
        public int Current { get; set; }
    }

    private State _state;

    public Parser(IEnumerable<Token> tokens)
    {
        _state.Current = 0;
        _tokens = tokens.ToList();
    }

    public Expression ParseExpression()
    {
        return ParseEquality();
    }

    // Todo: Factor out all similar functions into ParseBinop


    // ==, != 
    public Expression ParseEquality()
    {
        Expression left = ParseComparison();
        while (Match(TokenType.BangEqual, TokenType.EqualEqual))
        {
            Token op = PeekPrevious();
            Expression right = ParseComparison();
            left = op.Type switch
            {
                TokenType.EqualEqual => new Equality(left, right),
                TokenType.BangEqual => new Inequality(left, right),
                _ => throw new UnreachableException("Unreachable")
            };
        }

        return left;
    }

    // >=, >, <, <= 
    private Expression ParseComparison()
    {
        Expression left = ParseTerm();
        while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
        {
            Token op = PeekPrevious();
            Expression right = ParseTerm();
            left = op.Type switch
            {
                TokenType.Greater => new Greater(left, right),
                TokenType.GreaterEqual => new GreaterEqual(left, right),
                TokenType.Less => new Less(left, right),
                TokenType.LessEqual => new LessEqual(left, right),
                _ => throw new UnreachableException("Unreachable")
            };
        }

        return left;
    }


    // + -
    private Expression ParseTerm()
    {
        Expression left = ParseFactor();
        while (Match(TokenType.Minus, TokenType.Plus))
        {
            Token op = PeekPrevious();
            Expression right = ParseFactor();
            left = op.Type switch
            {
                TokenType.Plus => new Addition(left, right),
                TokenType.Minus => new Subtraction(left, right),
                _ => throw new UnreachableException("Unreachable")
            };
        }

        return left;
    }

    // * /
    private Expression ParseFactor()
    {
        Expression left = ParseUnary();
        while (Match(TokenType.Slash, TokenType.Star))
        {
            Token op = PeekPrevious();
            Expression right = ParseUnary();
            left = op.Type switch
            {
                TokenType.Star => new Multiplication(left, right),
                TokenType.Slash => new Division(left, right),
                _ => throw new UnreachableException("Unreachable")
            };
        }

        return left;
    }

    // - ! 
    private Expression ParseUnary()
    {
        if (Match(TokenType.Bang, TokenType.Minus))
        {
            Token op = PeekPrevious();

            return new Unary(ParseUnary(), op);
        }

        return ParsePrimary();
    }

    // number, string, nil, true, false
    private Expression ParsePrimary()
    {
        if (Match(TokenType.False)) return new Literal(false);
        if (Match(TokenType.True)) return new Literal(true);
        if (Match(TokenType.Nil)) return new Literal(null);

        if (Match(TokenType.Number, TokenType.String))
            return new Literal(PeekPrevious().Literal);

        if (!Match(TokenType.LeftParen))
        {
            // TODO: Proper error handling 
            throw new Exception($"Unexpected token: {PeekToken()}");
        }

        Expression expr = ParseExpression();
        if (!Match(TokenType.RightParen)) throw new Exception($"Expected ')'");
        return new Grouping(expr);
    }

    private bool Match(params Span<TokenType> types)
    {
        foreach (var type in types)
        {
            if (PeekToken().Type == type)
            {
                SkipToken();
                return true;
            }
        }

        return false;
    }

    private void SkipToken() => _ = NextToken();

    [Pure]
    private Token NextToken() => _tokens[_state.Current++];

    private Token PeekToken() => _tokens[_state.Current];

    private Token PeekPrevious() => _tokens[_state.Current - 1];

    private bool IsEof() => PeekToken().Type == TokenType.Eof;

    public State SaveState() => _state;
    private void RestoreState(State state) => _state = state;

    private bool ExpectAndConsume(TokenType expected)
    {
        if (IsEof()) return false;
        if (PeekToken().Type == expected)
        {
            SkipToken();
            return true;
        }

        Error(expected, PeekToken());

        return false;
    }

    private void Error(Span<TokenType> expected, Token got)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < expected.Length; i++)
        {
            if (i > 0)
            {
                if (i + 1 >= expected.Length) sb.Append(", or ");
                else sb.Append(", ");
            }

            sb.Append(expected[i].Humanize());
        }

        string str = sb.ToString();
        Util.Report(got.Location, $"Parse Error: expected {str}, but got {got.Type.Humanize()}");
    }

    private void Error(TokenType expected, Token got) => Error([expected], got);

    public static void Test()
    {
        var printer = new PrefixPrinter();
        var tokens = Lexer.FromFile("./Tests/parser.cslox").Accumulate().ToList();
        var self = new Parser(tokens);
        tokens.ForEach(Console.WriteLine);
        var expression = self.ParseExpression();

        Console.WriteLine(printer.Print(expression));
    }
}