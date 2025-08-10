using JetBrains.Annotations;
using Xunit;

namespace cslox.Tests;

[TestSubject(typeof(Parser))]
public class ParserTest
{
    private readonly ITestOutputHelper _output;

    public ParserTest(ITestOutputHelper output)
    {
        _output = output;
    }


    private Parser SetupParserFile(string filepath)
    {
        var tokens = Lexer.FromFile($"./Parser/{filepath}").Accumulate();
        return new Parser(tokens);
    }

    private Parser SetupParserString(string src, string? filePath = null)
    {
        var lexer = new Lexer(src, "<test>");
        return new Parser(lexer.Accumulate());
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
}