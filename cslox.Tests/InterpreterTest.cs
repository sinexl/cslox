using System;
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

    [Fact]
    public void Equality()
    {
        Assert.Equal(8.0, Interpret("4 + 4"));
        Assert.Equal(0.0, Interpret("4 - 4"));
    }

    [Fact]
    public void NilComparison()
    {
        Assert.Equal(true, Interpret("nil == nil"));
        Assert.Equal(false, Interpret("nil != nil"));
    }

    [Fact]
    public void ZeroDivision()
    {
        Assert.Throws<LoxZeroDivideException>(() => Interpret("4 / 0"));
    }

    [Fact]
    public void NegativeNumbersAndOrder()
    {
        Assert.Equal(-2.0, Interpret("-5 + 3"));
        Assert.Equal(-2.0, Interpret("3 + -5"));
        Assert.Equal(5.0, Interpret("--5")); // double negation
    }

    [Fact]
    public void FloatingPointPrecision()
    {
        var result = Interpret("0.1 + 0.2");
        Assert.IsType<double>(result);
        Assert.True(Math.Abs((double)result! - 0.3) < 1e-9, "Precision should be within epsilon");
    }

    [Fact]
    public void NilInArithmeticShouldThrow()
    {
        Assert.Throws<LoxCastException>(() => Interpret("nil + 1"));
        Assert.Throws<LoxCastException>(() => Interpret("1 - nil"));
    }

    [Fact]
    public void DivisionByNegative()
    {
        Assert.Equal(-2.0, Interpret("4 / -2"));
        Assert.Equal(-2.0, Interpret("-4 / 2"));
    }


    [Fact]
    public void ParenthesesPrecedence()
    {
        Assert.Equal(20.0, Interpret("(2 + 3) * 4"));
        Assert.Equal(14.0, Interpret("2 + 3 * 4")); // multiplication before addition
    }

    [Fact]
    public void LargeNumbers()
    {
        Assert.Equal(1_000_000_000_000.0, Interpret("1000000 * 1000000"));
    }

    public static object? Interpret(string src)
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