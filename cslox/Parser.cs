using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;
using cslox.Ast;
using cslox.Ast.Generated;

namespace cslox;

// TODO: Ternary operator.
/*
Syntax:
        program               → statement* EOF ;
        statement             → expressionStatement | printStatement ;
        expressionStatement   → expression ";" ;
        printStatement        → "print" expression ";" ;

        expression            → sequence ;
        sequence              → equality ( (  "," ) equality )* ;
        equality              → comparison ( ( "!=" | "==" ) comparison )* ;
        comparison            → term ( ( ">" | ">=" | "<" | "<=" ) term )* ;
        term                  → factor ( ( "-" | "+" ) factor )* ;
        factor                → unary ( ( "/" | "*" ) unary )* ;
        unary                 → ( "!" | "-" ) unary
                              | primary ;
        primary               → NUMBER | STRING | "true" | "false" | "nil"
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

    public Statement[]? Parse()
    {
        List<Statement> statements = new();
        while (!IsEof())
        {
            var statement = ParseStatement();
            if (statement is null) return null;
            statements.Add(statement);
        }

        return statements.ToArray();
    }

    public Statement? ParseStatement()
    {
        var state = SaveState();
        if (Match(TokenType.Print))
        {
            RestoreState(state);
            return ParsePrintStatement();
        }

        return ParseExpressionStatement();
    }


    public Statement? ParsePrintStatement()
    {
        SourceLocation location = PeekToken().Location;
        if (!ExpectAndConsume(TokenType.Print)) return null;
        Expression? expr = ParseExpression();
        if (!ExpectAndConsume(TokenType.Semicolon) || expr is null) return null;
        return new Print(expr) { Location = location };
    }

    public Statement? ParseExpressionStatement()
    {
        SourceLocation location = PeekToken().Location;
        Expression? expr = ParseExpression();
        if (!ExpectAndConsume(TokenType.Semicolon) || expr is null) return null;
        return new ExpressionStatement(expr) { Location = location };
    }


    // TODO: Parser should collect errors into Array/List of errors instead of reporting them on go 
    public Expression? ParseExpression()
    {
        return ParseSequence();
    }

    public Expression? ParseSequence()
    {
        SourceLocation loc = PeekToken().Location;
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
        return new Sequence(expressions.ToArray()) { Location = loc };
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
                TokenType.EqualEqual => new Equality(left, right) { Location = op.Location },
                TokenType.BangEqual => new Inequality(left, right) { Location = op.Location },
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
                TokenType.Greater => new Greater(left, right) { Location = op.Location },
                TokenType.GreaterEqual => new GreaterEqual(left, right) { Location = op.Location },
                TokenType.Less => new Less(left, right) { Location = op.Location },
                TokenType.LessEqual => new LessEqual(left, right) { Location = op.Location },
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
                TokenType.Plus => new Addition(left, right) { Location = op.Location },
                TokenType.Minus => new Subtraction(left, right) { Location = op.Location },
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
                TokenType.Star => new Multiplication(left, right) { Location = op.Location },
                TokenType.Slash => new Division(left, right) { Location = op.Location },
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

            return new Unary(inner, op) { Location = op.Location };
        }

        return ParsePrimary();
    }

    // number, string, nil, true, false
    private Expression? ParsePrimary()
    {
        SourceLocation loc = PeekToken().Location;
        if (Match(TokenType.False)) return CreateLiteral(false);
        if (Match(TokenType.True)) return CreateLiteral(true);
        if (Match(TokenType.Nil)) return CreateLiteral(null);

        if (Match(TokenType.Number, TokenType.String))
            return new Literal(PeekPrevious().Literal) { Location = loc };

        if (ExpectAndConsume(TokenType.LeftParen))
        {
            SourceLocation parenthesisLoc = PeekPrevious().Location;
            Expression? expr = ParseExpression();
            if (expr is null) return null;
            if (!ExpectAndConsume(TokenType.RightParen)) return null;
            return new Grouping(expr) { Location = parenthesisLoc };
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

    private Literal CreateLiteral(object? value) => new(value) { Location = PeekToken().Location };

    public static void Test()
    {
        var prefixPrinter = new PrefixPrinter();
        var locPrinter = new LocationPrinter();
        var tokens = Lexer.FromFile("./QuickTests/parser.cslox").Accumulate().ToList();
        var self = new Parser(tokens);
        tokens.ForEach(t => Console.WriteLine($"{t.Location}: {t.Type}"));
        Console.WriteLine("\n");
        var statements = self.Parse();

        if (statements is null)
        {
            Console.Error.WriteLine("Parse Error occured. Exiting...");
            Environment.Exit(1);
        }

        statements.ForEach(Console.WriteLine);
        // Console.WriteLine(locPrinter.Print(statements));
        // Console.WriteLine(prefixPrinter.Print(statements));
    }
}

public static class ParserExtensions
{
    public static Expression WithLoc(this Expression expr, SourceLocation loc)
    {
        expr.Location = loc;
        return expr;
    }
}