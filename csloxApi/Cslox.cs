using cslox;
using cslox.Runtime;

namespace csloxApi;

public class Cslox
{
    private Interpreter _interpreter;

    public Cslox(bool allowRedefinition)
    {
        _interpreter = new Interpreter();
    }

    public object? Evaluate(string expression)
    {
        var lexer = new Lexer("<cslox api>", expression);
        var tokens = lexer.Accumulate();
        foreach (var error in lexer.Errors)
        {
            throw new CsloxCompileError(error);
        }

        var parser = new Parser(tokens);
        var expr = parser.ParseExpression()!;
        foreach (var error in parser.Errors)
        {
            throw new CsloxCompileError(error);
        }

        object? result = null;
        try
        {
            result = _interpreter.Evaluate(expr);
        }
        catch (LoxRuntimeException)
        {
            throw;
        }

        return result;
    }
}

public class CsloxCompileError(Error error) : Exception
{
    public Error Error { get; set; } = error;
}