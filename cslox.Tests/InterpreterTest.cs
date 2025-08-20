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
        Assert.True(Math.Abs((double)result - 0.3) < 1e-9, "Precision should be within epsilon");
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

    [Fact]
    public static void ReadingFromUnexistingVariable()
    {
        string src = "print a;";
        Assert.Throws<LoxVariableUndefinedException>(() => InterpretStatements(src));
    }

    [Fact]
    public static void AssigningToVariables()
    {
        string src = "var a = 10; a = 11; print a;";
        var result = RecordInterpreterOutput(src).Trim();
        Assert.Equal("11", result);
    }

    [Fact]
    public static void AssigningToUnexistingVariable()
    {
        string src = "a = 10;";
        Assert.Throws<LoxVariableUndefinedException>(() => InterpretStatements(src));
    }

    [Fact]
    public static void Scoping()
    {
        var output = RecordInterpreterOutput(File.ReadAllText("./Interpreter/scoping.cslox"));
        var expected = File.ReadAllText("./Interpreter/scoping.expected.txt");
        Assert.Equal(expected, output);
    }

    [Fact]
    public static void VariableRedefinition()
    {
        Assert.Throws<LoxVariableUndefinedException>(() => InterpretStatements("var a = 10; var a = 11;"));
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("10 > 15", false)]
    [InlineData("10 < 15", true)]
    [InlineData("true and false or true", true)] // ((true and false) or true)
    [InlineData("true or false and false", true)] // (true or (false and false))
    [InlineData("false or false and true", false)] // (false or (false and true))
    [InlineData("false and true or true", true)] // ((false and true) or true)
    [InlineData("10 < 5 or 5 < 10 and 2 != 3", true)] // ((10 < 5) or ((5 < 10) and (2 != 3)))
    [InlineData("10 > 5 and 5 > 10 or 1 == 1", true)] // (((10 > 5) and (5 > 10)) or (1 == 1))
    [InlineData("10 > 5 or 5 > 10 and 1 != 1", true)] // ((10 > 5) or ((5 > 10) and (1 != 1)))
    [InlineData("10 < 5 and 5 < 10 or 2 == 3", false)] // (((10 < 5) and (5 < 10)) or (2 == 3))
    [InlineData("1 == 1 and 2 == 2 or 3 == 4", true)] // (((1 == 1) and (2 == 2)) or (3 == 4))
    [InlineData("1 != 1 and 2 == 2 or 3 == 3", true)] // (((1 != 1) and (2 == 2)) or (3 == 3))
    [InlineData("1 == 1 or 2 == 2 and 3 == 4", true)] // ((1 == 1) or ((2 == 2) and (3 == 4)))
    [InlineData("1 == 2 or 2 == 2 and 3 == 3", true)] // ((1 == 2) or ((2 == 2) and (3 == 3)))
    public static void If_Statement(string condition, bool result)
    {
        string src = $"""
                      if ({condition})  
                        print "true"; 
                      else print "false"; 
                      """;
        var output = RecordInterpreterOutput(src).Trim();
        Assert.Equal(result ? "true" : "false", output);
    }

    [Theory]
    [InlineData(
        "var a = 0",
        "a < 5",
        "{print a; a = a + 1;}",
        new[] { "0", "1", "2", "3", "4" }
    )]
    [InlineData(
        "var a = 5",
        "a > 0",
        "{print a; a = a - 1;}",
        new[] { "5", "4", "3", "2", "1" }
    )]
    [InlineData(
        "var a = 1",
        "a <= 16",
        "{print a; a = a * 2;}",
        new[] { "1", "2", "4", "8", "16" }
    )]
    [InlineData(
        "var a = 1",
        "a < 50",
        "{print a; a = a + a;}",
        new[] { "1", "2", "4", "8", "16", "32" }
    )]
    [InlineData(
        "var a = 10",
        "a > 2",
        "{print a; a = a / 2;}",
        new[] { "10", "5", "2.5" }
    )]
    public static void While_Loop(string init, string cond, string body, string[] resultLines)
    {
        string src = $"""
                      {init}; 
                      while ({cond}) 
                       {body}
                      """;
        var output = RecordInterpreterOutput(src).Trim().Split('\n');
        Assert.Equal(resultLines, output);
    }

    [Fact]
    public static void For_Loop_Total()
    {
        string src = "for (var i = 0; i < 10; i = i + 1) print i;";
        var output = RecordInterpreterOutput(src).Trim().Split('\n');
        Assert.Equal(["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"], output);
    }

    [Fact]
    public static void For_Loop_Infinite()
    {
        string src = """
                     var a = 0; 
                     for(;;) 
                     {
                        print a;
                        if (a == 10) break; 
                        a = a + 1;
                     } 
                     """;
        var output = RecordInterpreterOutput(src).Trim().Split('\n');
        Assert.Equal(["0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10"], output);
    }

    [Fact]
    public static void Break_Statement_TopLevel() =>
        Assert.Throws<LoxBreakException>(() => InterpretStatements("break;"));

    [Fact]
    public static void Break_Statement_InsideLoop()
    {
        var src = """
                  for(var i = 0; ;i = i + 1) 
                  {
                    if (i == 10) break;  
                    print i; 
                  }
                  """;
        var output = RecordInterpreterOutput(src).Trim().Split('\n');
        Assert.Equal(["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"], output);
    }

    [Fact]
    public static void Break_Statement_Nested()
    {
        var path = "./Interpreter/nested_break.cslox";
        var output = InterpretFileAndRecordOutput(path);
        Assert.DoesNotContain("This should never be printed", output);
    }

    [Fact]
    public static void Closures()
    {
        var path = "./Interpreter/closures.cslox";
        var output = InterpretFileAndRecordOutput(path);
        var expected = File.ReadAllText("./Interpreter/closures.expected.txt");
        Assert.Equal(expected, output);
    }

    [Fact]
    public static void Lambda()
    {
        var src =
            """
            var counter = 0; 
            var five = fun (f) {
                for(var i = 0; i < 5; i = i + 1) f(i);
            }; 
            five (fun (x) { counter = counter + x; });  
            print counter; 
            """;
        var output = RecordInterpreterOutput(src).Trim();
        Assert.Equal("10", output);
    }

    [Fact]
    public static void Lambda_Inline_Call()
    {
        var src = 
            """
            fun (name) { print "Hello " + name; }("Michael");
            """;
        var output = RecordInterpreterOutput(src).Trim(); 
        Assert.Equal("Hello Michael", output); 
    }


    private static string InterpretFileAndRecordOutput(string filepath)
    {
        var src = File.ReadAllText(filepath);
        return RecordInterpreterOutput(src);
    }

    private static string RecordInterpreterOutput(string src)
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
        var runner = new Runner("<testcase>") { AllowRedefinition = false };
        var (errors, exceptions) = runner.Run(src);
        foreach (var ex in exceptions)
            throw ex;
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