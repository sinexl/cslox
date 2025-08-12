using cslox.Runtime;

namespace cslox;

public class Runner
{
    public Interpreter Interpreter;
    public bool Report { get; set; } = false;

    private bool _allowRedefinition = false;

    public required bool AllowRedefinition
    {
        get => _allowRedefinition;
        set
        {
            Interpreter.Context.AllowRedefinition = value;
            _allowRedefinition = value;
        }
    }

    public string FilePath { get; init; }

    public Runner(string filepath)
    {
        FilePath = filepath;
        Interpreter = new();
    }

    public (Error[], LoxRuntimeException[]) Run(string src)
    {
        List<Error> errors = [];

        var lexer = new Lexer(src, FilePath);
        var tokens = lexer.Accumulate();
        // tokens.ForEach(Console.WriteLine);
        errors.AddRangeAndReport(lexer.Errors, Report);

        var parser = new Parser(tokens);
        var statements = parser.Parse();
        if (statements is null) return (errors.ToArray(), []);
        errors.AddRangeAndReport(parser.Errors, Report);

        List<LoxRuntimeException> runtimeErrors = [];
        try
        {
            foreach (var statement in statements)
            {
                Interpreter.Execute(statement);
            }
        }
        catch (LoxRuntimeException e)
        {
            runtimeErrors.Add(e);
        }


        return (errors.ToArray(), runtimeErrors.ToArray());
    }
}

public static class RunnerExtensions
{
    public static void AddRangeAndReport(this List<Error> errors, IList<Error> errorsToAdd, bool doReport)
    {
        errors.AddRange(errorsToAdd);
        if (!doReport) return;
        foreach (var error in errorsToAdd)
        {
            Console.Error.WriteLine(error);
        }
    }

    public static bool Any(this (IList<Error>, IList<LoxRuntimeException>) errors)
    {
        return errors.Item1.Any() || errors.Item2.Any();
    }
}