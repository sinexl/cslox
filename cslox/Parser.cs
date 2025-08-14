using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text;
using cslox.Ast;
using cslox.Ast.Generated;

namespace cslox;

// TODO: Ternary operator.

// ReSharper disable once GrammarMistakeInComment
/*
Syntax:
        program               → declaration* EOF;
        declaration           → varDeclaration | statement
        varDeclaration        → "var" IDENTIFIER ( "=" expression)? ";" ;
        statement             → expressionStatement | printStatement | blockStatement | ifStatement ;
        blockStatement        → "{" declaration* "}" ;
        expressionStatement   → expression ";" ;
        printStatement        → "print" expression ";" ;
        ifStatement           → "if" "(" expression ")" statement ( "else" statement )? ;

        expression            → sequence ;
        sequence              → assignment ( (  "," ) assignment )* ;
        assignment            → IDENTIFIER "=" assignment
                                | LogicalOr ;
        LogicalOr             →  logicalAnd ( "or" logicalAnd)* ; 
        LogicalAnd            →  equality ( "and" equality)* ; 
        equality              → comparison ( ( "!=" | "==" ) comparison )* ;
        comparison            → term ( ( ">" | ">=" | "<" | "<=" ) term )* ;
        term                  → factor ( ( "-" | "+" ) factor )* ;
        factor                → unary ( ( "/" | "*" ) unary )* ;
        unary                 → ( "!" | "-" ) unary
                              | primary ;
        primary               → NUMBER | STRING | "true" | "false" | "nil" | IDENTIFIER
                              | "(" expression ")" ;

Precedence & Associativity  (from the highest precedence to lowest)
        Name          Operators      Associates
        ------------------------------------
        Unary          ! -           |  Right
        Factor         / *           |  Left
        Term           - +           |  Left
        Comparison     > >= < <=     |  Left
        Equality       == !=         |  Left
        LogicalAnd    and            |  Left 
        LogicalOr     or             |  Left 
        Assignment     =             |  Right
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

    public List<Error> Errors { get; } = new();

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
            var statement = ParseDeclaration();
            if (statement is null) return null;
            statements.Add(statement);
        }

        return statements.ToArray();
    }

    // Statements
    public Statement? ParseDeclaration()
    {
        var state = SaveState();
        if (Match(TokenType.Var))
        {
            RestoreState(state);
            return ParseVariableDeclaration() ?? SyncAndNull<Statement>();
        }

        return ParseStatement() ?? SyncAndNull<Statement>();
    }

    private Statement? ParseVariableDeclaration()
    {
        SourceLocation location = PeekToken().Location;
        if (!ExpectAndConsume(TokenType.Var)) return null;
        if (!ExpectAndConsume(TokenType.Identifier, out var token)) return null;
        Debug.Assert(token.Type == TokenType.Identifier);
        string name = token.Lexeme;
        Expression? initializer = null;
        if (Match(TokenType.Equal))
        {
            initializer = ParseExpression();
            if (initializer is null) return null;
        }

        if (!ExpectAndConsume(TokenType.Semicolon)) return null;
        return new VarDeclaration(name, initializer) { Location = location };
    }

    public Statement? ParseStatement()
    {
        var state = SaveState();
        if (Match(TokenType.Print))
        {
            RestoreState(state);
            return ParsePrintStatement();
        }

        if (Match(TokenType.LeftBrace))
        {
            RestoreState(state);
            return ParseBlockStatement();
        }
        
        if (Match(TokenType.If))
        {
            RestoreState(state);
            return ParseIfStatement(); 
        }

        return ParseExpressionStatement();
    }

    private Statement? ParseIfStatement()
    {
        if (!ExpectAndConsume(TokenType.If, out var ifToken)) return null;
        Debug.Assert(ifToken.Type == TokenType.If);
        SourceLocation ifLoc = ifToken.Location; 
        
        if (!ExpectAndConsume(TokenType.LeftParen)) return null; 
        Expression? condition = ParseExpression(); 
        if (condition is null) return null; 
        if (!ExpectAndConsume(TokenType.RightParen)) return null;  
        
        Statement? thenBranch = ParseStatement();  
        if (thenBranch is null) return null;   
        
        Statement? elseBranch = null; 
        if (Match(TokenType.Else))
        {
            elseBranch = ParseStatement();
            if (elseBranch is null) return null;
        } 
        
        return new If(condition, thenBranch, elseBranch) { Location = ifLoc }; 
    }

    private Statement? ParseBlockStatement()
    {
        List<Statement> statements = new();
        if (!ExpectAndConsume(TokenType.LeftBrace, out var leftBrace)) return null;
        Debug.Assert(leftBrace.Type == TokenType.LeftBrace);
        SourceLocation leftBraceLoc = leftBrace.Location;

        while (PeekToken().Type != TokenType.RightBrace && !IsEof())
        {
            var statement = ParseDeclaration();
            if (statement is null) return null;
            statements.Add(statement);
        }

        if (!ExpectAndConsume(TokenType.RightBrace)) return null;
        // if (statements.Count == 0) TODO: Empty statement
        
        // With this condition we make { statement; } equal to statement; 
        if (statements.Count == 1) return statements[0];
        return new Block(statements.ToArray()) { Location = leftBraceLoc };
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


    // Expressions
    public Expression? ParseExpression()
    {
        return ParseSequence();
    }

    public Expression? ParseSequence()
    {
        SourceLocation loc = PeekToken().Location;
        List<Expression> expressions = new();
        var item = ParseAssignment();
        if (item is null) return null;
        expressions.Add(item);
        while (Match(TokenType.Comma))
        {
            item = ParseAssignment();
            if (item is null) return null;
            expressions.Add(item);
        }

        if (expressions.Count == 1) return expressions[0];
        return new Sequence(expressions.ToArray()) { Location = loc };
    }

    public Expression? ParseAssignment()
    {
        Expression? target = ParseLogicalOr();
        if (target is null) return null;
        if (Match(TokenType.Equal))
        {
            Token eq = PeekPrevious();
            Expression? value = ParseAssignment();
            if (value is null) return null;

            if (target is ReadVariable(var name))
                return new Assign(name, value) { Location = eq.Location };

            Error(eq.Location, "Invalid assignment target");
            return null;
        }

        return target;
    }

    private Expression? ParseLogicalOr()
    {
        Expression? left = ParseLogicalAnd();
        if (left is null) return null;

        while (Match(TokenType.Or))
        {
            Token op = PeekPrevious(); 
            SourceLocation loc = op.Location; 
            
            Expression? right = ParseLogicalAnd();
            if (right is null) return null;
            left = new LogicalOr(left, right) { Location = loc }; 
        }

        return left;
    }

    private Expression? ParseLogicalAnd()
    {
        Expression? left = ParseEquality();
        if (left is null) return null; 
        while (Match(TokenType.And))
        {
            Token op = PeekPrevious();
            SourceLocation loc = op.Location; 
            
            Expression? right = ParseEquality(); 
            if (right is null) return null; 
            left = new LogicalAnd(left, right) { Location = loc }; 
        }

        return left; 
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

        if (Match(TokenType.Identifier)) return new ReadVariable(PeekPrevious().Lexeme) { Location = loc };
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
    private bool ExpectAndConsume(TokenType expected, out Token result)
    {
        if (PeekToken().Type == expected)
        {
            result = PeekToken();
            SkipToken();
            return true;
        }

        Error(expected, PeekToken());
        result = PeekToken();
        return false;
    }

    [Pure]
    private bool ExpectAndConsume(TokenType expected) => ExpectAndConsume(expected, out _);

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
        Errors.Add(new Error(got.Location,
            message is not null
                ? $"error: {message}. Expected {str}, but got {got.Type.Humanize()}"
                : $"error: expected {str}, but got {got.Type.Humanize()}"));
    }

    private void Error(TokenType expected, Token got, string? message = null) => Error([expected], got, message);
    private void Error(SourceLocation location, string message) => Errors.Add(new Error(location, $"error: {message}"));

    private T? SyncAndNull<T>()
    {
        SynchronizeToStatement();
        return default;
    }

    // As suggested in the book, we use statements as synchronization point. 
    // When an error occurs, we treat the current statement as malformed and skip all tokens until the next statement 
    private void SynchronizeToStatement()
    {
        if (IsEof()) return;
        SkipToken();
        while (!IsEof())
        {
            if (PeekPrevious().Type == TokenType.Semicolon) return;
            if (PeekToken().Type.IsStatementBeginning()) return;
            SkipToken();
        }
    }

    private Literal CreateLiteral(object? value) => new(value) { Location = PeekPrevious().Location };

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
            foreach (var error in self.Errors)
            {
                Console.WriteLine(error);
            }

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