using cslox;
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
        Assert.Equal(Interpret("4 + 4"), 8.0);
        Assert.Equal(Interpret("4 - 4"), 0.0);
    }

    [Fact]
    public void NilComparison()
    {
        Assert.Equal(Interpret("nil == nil"), true);
        Assert.Equal(Interpret("nil != nil"), false); 
    }

    [Fact]
    public void ZeroDivision()
    {
        Assert.Throws<LoxZeroDivideException>(() => Interpret("4 / 0"));
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