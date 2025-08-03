using System.Diagnostics.Contracts;
using System.Globalization;
using System.Numerics;
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
    private int _current;

    public Parser(IEnumerable<Token> tokens)
    {
        _current = 0;
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
            // left = new Binary(left, right, op);
            switch (op.Type) 
            {
                // case TokenType.EqualEqual: 
                //     left = new (left, right); break; 
                // case TokenType.BangEqual:  
                //     left = new 
            } 
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
            // left = new Binary(left, right, op);
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
            // left = new Binary(left, right, op);
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
            // left = new Binary(left, right, op);
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
        {
            return new Literal(PeekToken().Literal);
        }

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

    private void SkipToken()
    {
        _ = NextToken();
    }

    [Pure]
    private Token NextToken()
    {
        return _tokens[_current++];
    }

    private Token PeekToken()
    {
        return _tokens[_current];
    }

    private Token PeekPrevious()
    {
        return _tokens[_current - 1];
    }

    private bool IsEof() => PeekToken().Type == TokenType.Eof;

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