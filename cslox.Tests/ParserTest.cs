using System;
using cslox.Ast;
using JetBrains.Annotations;
using Xunit;

namespace cslox.Tests;

[TestSubject(typeof(Parser))]
public class ParserTest
{
    private readonly ITestOutputHelper _output;
    private static PrefixPrinter _printer = new();

    public ParserTest(ITestOutputHelper output)
    {
        _output = output;
    }

    private static Parser SetupParserFile(string filepath)
    {
        var tokens = Lexer.FromFile($"./Parser/{filepath}").Accumulate();
        return new Parser(tokens);
    }

    private static Parser SetupParserString(string src, string? filePath = null)
    {
        var lexer = new Lexer(src, "<test>");
        return new Parser(lexer.Accumulate());
    }

    private static string Parse(string src)
    {
        var parser = SetupParserString(src);
        var expression = parser.ParseExpression();
        return _printer.Print(expression ?? throw new ArgumentNullException());
    }

    [Fact]
    public void TestExpressionLocations()
    {
        var path = "./locations.cslox";
        var parser = SetupParserFile(path);
        var expression = parser.ParseExpression();
        Assert.NotNull(expression);
        string str = expression.ToString()!;
        Assert.NotNull(str);
        _output.WriteLine(str);
    }

    [Theory]
    [InlineData("1 + 3 * 4", "(+ 1 (* 3 4))")]
    public void TestPrecedence(string src, string expected)
    {
        Assert.Equal(expected, Parse(src));
    }

    [Theory]
    [InlineData("123", "123")]
    [InlineData("\"hello\"", "\"hello\"")]
    public void Primary_Number_And_String(string src, string expected)
    {
        Assert.Equal(expected, Parse(src));
    }

    [Theory]
    [InlineData("true", "true")]
    [InlineData("false", "false")]
    [InlineData("nil", "nil")]
    public void Primary_Literals(string src, string expected)
    {
        Assert.Equal(expected, Parse(src));
    }

    [Theory]
    [InlineData("(42)", "(group 42)")]
    [InlineData("(1 + 2)", "(group (+ 1 2))")]
    public void Primary_Grouping(string src, string expected)
    {
        Assert.Equal(expected, Parse(src));
    }

    [Theory]
    [InlineData("!true", "(! true)")]
    [InlineData("-5", "(- 5)")]
    [InlineData("!-5", "(! (- 5))")]
    public void Unary_Not_And_Negative(string src, string expected)
    {
        Assert.Equal(expected, Parse(src));
    }

    [Theory]
    [InlineData("2 * 3", "(* 2 3)")]
    [InlineData("10 / 5", "(/ 10 5)")]
    [InlineData("10 / 5 * 2", "(* (/ 10 5) 2)")]
    public void Factor_Multiplication_And_Division(string src, string expected)
    {
        Assert.Equal(expected, Parse(src));
    }

    [Theory]
    [InlineData("1 + 2", "(+ 1 2)")]
    [InlineData("4 - 3", "(- 4 3)")]
    [InlineData("10 - 5 + 2", "(+ (- 10 5) 2)")]
    public void Term_Addition_And_Subtraction(string src, string expected)
    {
        Assert.Equal(expected, Parse(src));
    }

    [Theory]
    [InlineData("1 < 2", "(< 1 2)")]
    [InlineData("2 <= 3", "(<= 2 3)")]
    [InlineData("4 > 3", "(> 4 3)")]
    [InlineData("5 >= 5", "(>= 5 5)")]
    public void Comparison_Operators(string src, string expected)
    {
        Assert.Equal(expected, Parse(src));
    }

    [Theory]
    [InlineData("1 == 1", "(== 1 1)")]
    [InlineData("1 != 2", "(!= 1 2)")]
    public void Equality_Operators(string src, string expected)
    {
        Assert.Equal(expected, Parse(src));
    }

    [Theory]
    [InlineData("1, 2, 3", "(sequence 1 2 3)")]
    [InlineData("1 + 2, 3 * 4", "(sequence (+ 1 2) (* 3 4))")]
    public void Sequence_With_Comma(string src, string expected)
    {
        Assert.Equal(expected, Parse(src));
    }

    [Theory]
    [InlineData("(1 + 2) * 3 == 9, 4 < 5", "(sequence (== (* (group (+ 1 2)) 3) 9) (< 4 5))")]
    public void Complex_Expression_FullPrecedence(string src, string expected)
    {
        Assert.Equal(expected, Parse(src));
    }
}