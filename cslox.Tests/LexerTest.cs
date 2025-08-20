using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Xunit;

namespace cslox.Tests;

[TestSubject(typeof(Lexer))]
public class LexerTest
{
    private readonly ITestOutputHelper _output;

    public LexerTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Identifiers()
    {
        var tokens = Lexer.FromFile($"./Lexer/ids.cslox").Accumulate();
        tokens.ForEach(x => _output.WriteLine(x.ToString()));
    }

    [Fact]
    public void Types()
    {
        string src =
            """
            1 hello * /*comment*/ (2 + 3)
            hello /* nested /* multi 
              line */ comment */ 123 helo
            // this is a comment 
            /* this is also 
                  a comment */ 
            (( )){} // grouping stuff
            !*+-/=<> <= == // operators
            "String Literal" 474 bog
            // 
            """;

        var types = LexTokenTypes(src);
        TokenType[] expected =
        [
            TokenType.Number, TokenType.Identifier, TokenType.Star, TokenType.LeftParen, TokenType.Number,
            TokenType.Plus, TokenType.Number, TokenType.RightParen, TokenType.Identifier, TokenType.Number,
            TokenType.Identifier, TokenType.LeftParen, TokenType.LeftParen, TokenType.RightParen, TokenType.RightParen,
            TokenType.LeftBrace, TokenType.RightBrace, TokenType.Bang, TokenType.Star, TokenType.Plus, TokenType.Minus,
            TokenType.Slash, TokenType.Equal, TokenType.Less, TokenType.Greater, TokenType.LessEqual,
            TokenType.EqualEqual, TokenType.String, TokenType.Number, TokenType.Identifier, TokenType.Eof,
        ];
        Assert.Equal(expected, types);
    }

    public static IList<TokenType> LexTokenTypes(string src)
    {
        var lexer = new Lexer(src, "<test>"); 
        var tokens = lexer.Accumulate(); 
        Assert.Empty(lexer.Errors);
        
        return tokens.Select(t => t.Type).ToArray();
    }
}