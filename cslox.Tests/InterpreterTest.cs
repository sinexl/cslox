using System;
using System.IO;
using cslox.Runtime;
using JetBrains.Annotations;
using Xunit;

namespace cslox.Tests;

[TestSubject(typeof(Interpreter))]
public class InterpreterTest
{
    private readonly ITestOutputHelper _output;

    public InterpreterTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData("4 + 4", 8.0)]
    [InlineData("4 - 4", 0.0)]
    public void Equality(string expr, double expected)
    {
        Assert.Equal(expected, InterpretExpr(expr));
    }

    [Theory]
    [InlineData("nil == nil", true)]
    [InlineData("nil != nil", false)]
    public void NilComparison(string expr, bool expected)
    {
        Assert.Equal(expected, InterpretExpr(expr));
    }

    [Fact]
    public void ZeroDivision()
    {
        Assert.Throws<LoxZeroDivideException>(() => InterpretExpr("4 / 0"));
    }

    [Theory]
    [InlineData("-5 + 3", -2.0)]
    [InlineData("3 + -5", -2.0)]
    [InlineData("--5", 5.0)]
    public void NegativeNumbersAndOrder(string expr, double expected)
    {
        Assert.Equal(expected, InterpretExpr(expr));
    }

    [Fact]
    public void FloatingPointPrecision()
    {
        var result = InterpretExpr("0.1 + 0.2");
        Assert.IsType<double>(result);
        Assert.True(Math.Abs((double)result! - 0.3) < 1e-9, "Precision should be within epsilon");
    }

    [Theory]
    [InlineData("nil + 1")]
    [InlineData("1 - nil")]
    public void NilInArithmeticShouldThrow(string expr)
    {
        Assert.Throws<LoxCastException>(() => InterpretExpr(expr));
    }

    [Theory]
    [InlineData("4 / -2", -2.0)]
    [InlineData("-4 / 2", -2.0)]
    public void DivisionByNegative(string expr, double expected)
    {
        Assert.Equal(expected, InterpretExpr(expr));
    }

    [Theory]
    [InlineData("(2 + 3) * 4", 20.0)]
    [InlineData("2 + 3 * 4", 14.0)]
    public void ParenthesesPrecedence(string expr, double expected)
    {
        Assert.Equal(expected, InterpretExpr(expr));
    }

    [Theory]
    [InlineData("1000000 * 1000000", 1_000_000_000_000.0)]
    public void LargeNumbers(string expr, double expected)
    {
        Assert.Equal(expected, InterpretExpr(expr));
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("some")]
    public static void PrintUnexistingVariable(string varName)
    {
        Assert.Throws<LoxVariableUndefinedException>(() => InterpretStatements($"print {varName};"));
    }

    [Fact]
    public static void ReadingFromVariables()
    {
        string src = """
                     var b = 10; 
                     print b; 
                     """;
        var result = RecordInterpreterOutput(src).Trim(); 
        Assert.Equal("10", result);
    }

    public static string RecordInterpreterOutput(string src)
    {
        using var sw = new StringWriter();
        var original = Console.Out;
        Console.SetOut(sw);
        try
        {
            InterpretStatements(src);
        }
        finally
        {
            Console.SetOut(original);
        }
        return sw.ToString(); 
    }

    private static void InterpretStatements(string src)
    {
        var lexer = new Lexer(src, "<testcase>");
        Assert.Empty(lexer.Errors);
        var parser = new Parser(lexer.Accumulate());
        var statements = parser.Parse();
        Assert.NotNull(statements);
        var interpreter = new Interpreter();
        foreach (var c in statements)
        {
            interpreter.Execute(c);
        }
    }

    private static object? InterpretExpr(string src)
    {
        var lexer = new Lexer(src, "<testcase>");
        Assert.Empty(lexer.Errors);
        var parser = new Parser(lexer.Accumulate());
        var expression = parser.ParseExpression();
        Assert.NotNull(expression);
        var interpreter = new Interpreter();
        return interpreter.Evaluate(expression);
    }
}