using cslox;
using cslox.Ast.Generated;
using cslox.Runtime;

namespace csloxApi;

public class Cslox
{
    private Interpreter _interpreter;

    public Cslox(bool allowRedefinition = true)
    {
        _interpreter = new Interpreter
        {
            Context =
            {
                AllowRedefinition = allowRedefinition
            }
        };
    }

    public object? Evaluate(string expression)
    {
        var lexer = new Lexer(expression, "<cslox api>");
        var tokens = lexer.Accumulate();
        foreach (var error in lexer.Errors)
        {
            throw new CsloxCompileError(error);
        }

        var parser = new Parser(tokens);
        var statements = parser.Parse()!;
        foreach (var error in parser.Errors)
        {
            throw new CsloxCompileError(error);
        }

        var resolver = new Resolver(_interpreter);
        resolver.Resolve(statements);

        if (statements is [ExpressionStatement expr])
        {
            object? result = _interpreter.Evaluate(expr.Expression);
            return result;
        }

        foreach (var s in statements)
            _interpreter.Execute(s);

        return null;
    }
}

public class CsloxCompileError(Error error) : Exception
{
    public Error Error { get; set; } = error;
    public override string ToString() => Error.ToString();
}