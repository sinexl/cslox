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

    [Fact]
    public void TestPrecedence()
    {
        Assert.Equal("(+ 1 (* 3 4))", Parse("1 + 3 * 4"));
    }

    [Fact]
    public void Primary_Number_And_String()
    {
        Assert.Equal("123", Parse("123"));
        Assert.Equal("\"hello\"", Parse("\"hello\""));
    }

    [Fact]
    public void Primary_Literals()
    {
        Assert.Equal("true", Parse("true"));
        Assert.Equal("false", Parse("false"));
        Assert.Equal("nil", Parse("nil"));
    }

    [Fact]
    public void Primary_Grouping()
    {
        Assert.Equal("(group 42)", Parse("(42)"));
        Assert.Equal("(group (+ 1 2))", Parse("(1 + 2)"));
    }

    [Fact]
    public void Unary_Not_And_Negative()
    {
        Assert.Equal("(! true)", Parse("!true"));
        Assert.Equal("(- 5)", Parse("-5"));
        Assert.Equal("(! (- 5))", Parse("!-5"));
    }

    [Fact]
    public void Factor_Multiplication_And_Division()
    {
        Assert.Equal("(* 2 3)", Parse("2 * 3"));
        Assert.Equal("(/ 10 5)", Parse("10 / 5"));
        Assert.Equal("(* (/ 10 5) 2)", Parse("10 / 5 * 2"));
    }

    [Fact]
    public void Term_Addition_And_Subtraction()
    {
        Assert.Equal("(+ 1 2)", Parse("1 + 2"));
        Assert.Equal("(- 4 3)", Parse("4 - 3"));
        Assert.Equal("(+ (- 10 5) 2)", Parse("10 - 5 + 2"));
    }

    [Fact]
    public void Comparison_Operators()
    {
        Assert.Equal("(< 1 2)", Parse("1 < 2"));
        Assert.Equal("(<= 2 3)", Parse("2 <= 3"));
        Assert.Equal("(> 4 3)", Parse("4 > 3"));
        Assert.Equal("(>= 5 5)", Parse("5 >= 5"));
    }

    [Fact]
    public void Equality_Operators()
    {
        Assert.Equal("(== 1 1)", Parse("1 == 1"));
        Assert.Equal("(!= 1 2)", Parse("1 != 2"));
    }
    [Fact]
    public void Sequence_With_Comma()
    {
        Assert.Equal("(sequence 1 2 3)", Parse("1, 2, 3"));
        Assert.Equal("(sequence (+ 1 2) (* 3 4))", Parse("1 + 2, 3 * 4"));
    }

    [Fact]
    public void Complex_Expression_FullPrecedence()
    {
        string src = "(1 + 2) * 3 == 9, 4 < 5";
        Assert.Equal("(sequence (== (* (group (+ 1 2)) 3) 9) (< 4 5))", Parse(src));
    }
}