using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;
using cslox.Ast;
using cslox.Ast.Generated;

namespace cslox;

// TODO: Ternary operator.
/*
Syntax:
        expression     → sequence ;
        sequence       → equality ( (  "," ) equality )* ;
        equality       → comparison ( ( "!=" | "==" ) comparison )* ;
        comparison     → term ( ( ">" | ">=" | "<" | "<=" ) term )* ;
        term           → factor ( ( "-" | "+" ) factor )* ;
        factor         → unary ( ( "/" | "*" ) unary )* ;
        unary          → ( "!" | "-" ) unary
                       | primary ;
        primary        → NUMBER | STRING | "true" | "false" | "nil"
                       | "(" expression ")" ;

Precedence & Associativity  (from the highest precedence to lowest)
        Name          Operators      Associates
        ------------------------------------
        Unary          ! -           |  Right
        Factor         / *           |  Left
        Term           - +           |  Left
        Comparison     > >= < <=     |  Left
        Equality       == !=         |  Left
        Sequence       ,             |  Left
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

    public Expression? ParseExpression()
    {
        return ParseSequence();
    }

    public Expression? ParseSequence()
    {
        List<Expression> expressions = new();
        var item = ParseEquality();
        if (item is null) return null;
        expressions.Add(item);
        while (Match(TokenType.Comma))
        {
            item = ParseEquality();
            if (item is null) return null;
            expressions.Add(item);
        }

        if (expressions.Count == 1) return expressions[0];
        return new Sequence(expressions.ToArray());
    }

    // Todo: Factor out all similar functions into ParseBinop


    // ==, != 
    public Expression? ParseEquality()
    {
        Expression? left = ParseComparison();
        if (left is null) return null;
        while (Match(TokenType.BangEqual, TokenType.EqualEqual))
        {
            Token op = PeekPrevious();
            Expression? right = ParseComparison();
            if (right is null) return null;
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
    private Expression? ParseComparison()
    {
        Expression? left = ParseTerm();
        if (left is null) return null;

        while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
        {
            Token op = PeekPrevious();
            Expression? right = ParseTerm();
            if (right is null) return null;
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
    private Expression? ParseTerm()
    {
        Expression? left = ParseFactor();
        if (left is null) return null;
        while (Match(TokenType.Minus, TokenType.Plus))
        {
            Token op = PeekPrevious();
            Expression? right = ParseFactor();
            if (right is null) return null;
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
    private Expression? ParseFactor()
    {
        Expression? left = ParseUnary();
        if (left is null) return null;
        while (Match(TokenType.Slash, TokenType.Star))
        {
            Token op = PeekPrevious();
            Expression? right = ParseUnary();
            if (right is null) return null;
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
    private Expression? ParseUnary()
    {
        if (Match(TokenType.Bang, TokenType.Minus))
        {
            Token op = PeekPrevious();
            Expression? inner = ParseUnary();
            if (inner is null) return null;

            return new Unary(inner, op);
        }

        return ParsePrimary();
    }

    // number, string, nil, true, false
    private Expression? ParsePrimary()
    {
        if (Match(TokenType.False)) return new Literal(false);
        if (Match(TokenType.True)) return new Literal(true);
        if (Match(TokenType.Nil)) return new Literal(null);

        if (Match(TokenType.Number, TokenType.String))
            return new Literal(PeekPrevious().Literal);

        if (ExpectAndConsume(TokenType.LeftParen))
        {
            Expression? expr = ParseExpression();
            if (expr is null) return null;
            if (!ExpectAndConsume(TokenType.RightParen)) return null;
            return new Grouping(expr);
        }

        Error(PeekToken().Location, "Expected expression");
        return null;
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

    [Pure]
    private bool ExpectAndConsume(TokenType expected)
    {
        if (PeekToken().Type == expected)
        {
            SkipToken();
            return true;
        }

        Error(expected, PeekToken());
        return false;
    }

    private void Error(Span<TokenType> expected, Token got, string? message = null)
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
        Util.Report(got.Location,
            message is not null
                ? $"error: {message}. Expected {str}, but got {got.Type.Humanize()}"
                : $"error: expected {str}, but got {got.Type.Humanize()}");
    }

    private void Error(TokenType expected, Token got, string? message = null) => Error([expected], got, message);
    private void Error(SourceLocation location, string message) => Util.Report(location, $"error: {message}");

    // As suggested in the book, we use statements as synchronization point. 
    // When an error occurs, we treat the current statement as malformed and skip all tokens until the next statement 
    private void SynchronizeToStatement()
    {
        SkipToken();
        while (!IsEof())
        {
            if (PeekPrevious().Type == TokenType.Semicolon) return;
            if (PeekToken().Type.IsStatementBeginning()) return;
            SkipToken();
        }
    }

    public static void Test()
    {
        var prefixPrinter = new PrefixPrinter();
        var tokens = Lexer.FromFile("./Tests/parser.cslox").Accumulate().ToList();
        var self = new Parser(tokens);
        tokens.ForEach(Console.WriteLine);
        var expression = self.ParseExpression();

        if (expression is null)
        {
            Console.Error.WriteLine("Parse Error occured. Exiting...");
            Environment.Exit(1);
        }

        Console.WriteLine(expression);
        Console.WriteLine(prefixPrinter.Print(expression));
    }
}