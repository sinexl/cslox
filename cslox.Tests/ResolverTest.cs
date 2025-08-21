using System.Collections.Generic;
using System.Linq;
using cslox.Ast.Generated;
using cslox.Runtime;
using JetBrains.Annotations;
using Xunit;

namespace cslox.Tests;

[TestSubject(typeof(Resolver))]
public class ResolverTest
{
    [Fact]
    public void VariableRedefinition()
    {
        var errors = ResolveBlockErrors("var a = 10; var a = 11;");
        Assert.Collection(errors,
            e => Assert.IsType<VariableRedefinition>(e));
    }

    [Fact]
    public void ReadingFromInitializer()
    {
        var errors = ResolveBlockErrors("var a = a;");
        Assert.Collection(errors,
            e => Assert.IsType<ReadingFromInitializer>(e));
    }

    [Fact]
    public void TopLevelReturn()
    {
        var src = "return 10;";
        var errors = ResolveBlockErrors(src);
        Assert.Collection(errors,
            e => Assert.IsType<TopLevelReturn>(e));
    }

    [Fact]
    public void TopLevelThis()
    {
        Assert.Collection(ResolveBlockErrors("this.ItemA = 10;"),
            e => Assert.IsType<UsingThisOutsideOfMethod>(e));
    }

    [Fact]
    public void ThisInFunction()
    {
        Assert.Collection(ResolveBlockErrors("fun a () { this.Field = 10; } "),
            e => Assert.IsType<UsingThisOutsideOfMethod>(e));
    }

    [Fact]
    public void ProperThisUsage()
    {
        var src =
            """
            class SomeClass 
            { method () { this.a = 10; } }
            var obj = SomeClass(); 
            obj.method(); 
            """;
        var resolutions = ResolveBlock(src);
        var expected = new Dictionary<(int, int), int>
        {
            [(2, 15)] = 1,
            [(3, 11)] = 0,
            [(4, 1)] = 0,
        };
        Assert.Equal(expected, resolutions);
    }

    [Fact]
    public void WithScopeMutation()
    {
        var src =
            """
            var a = "global";
            {
              fun showA() { print a; }

              showA();
              var a = "block";
              showA();
            }
            """;
        var expected = new Dictionary<(int, int), int>
        {
            [(3, 23)] = 2,
            [(5, 3)] = 0,
            [(7, 3)] = 0
        };
        var resolutions = ResolveBlock(src);
        Assert.Equal(expected, resolutions);
    }


    public static IList<Error> ResolveBlockErrors(string src)
    {
        var lexer = new Lexer(src, "<testcase>");
        var tokens = lexer.Accumulate();
        Assert.Empty(lexer.Errors);

        var parser = new Parser(tokens);
        var statements = parser.Parse();
        Assert.Empty(parser.Errors);
        Assert.NotNull(statements);

        var block = new Block(statements);
        var resolver = new Resolver(new Interpreter());
        resolver.Resolve(block);
        return resolver.Errors;
    }

    public Dictionary<(int, int), int> ResolveBlock(string src)
    {
        var lexer = new Lexer(src, "<testcase>");
        var tokens = lexer.Accumulate();
        Assert.Empty(lexer.Errors);

        var parser = new Parser(tokens);
        var statements = parser.Parse();
        Assert.Empty(parser.Errors);
        Assert.NotNull(statements);

        var block = new Block(statements);
        var interpreter = new Interpreter();
        var resolver = new Resolver(interpreter);
        resolver.Resolve(block);
        return interpreter.Locals
            .Select(l
                => (Loc: (l.Key.Location.Line, l.Key.Location.Offset), Val: l.Value))
            .ToDictionary(x => x.Loc, x => x.Val);
    }
}