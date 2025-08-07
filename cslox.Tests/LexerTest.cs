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
}