using cslox.Ast.Generated;
using cslox.Runtime;

namespace cslox;

public class Runner
{
    // Events 
    public event Action<Token[], IList<Error>>? OnTokenizerFinish;

    public event Action<IList<Statement>?, IList<Error>>? OnParserFinish;

    // Fields 
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

    public bool Dry { get; set; } = false;

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
        OnTokenizerFinish?.Invoke(tokens, lexer.Errors);
        errors.AddRangeAndReport(lexer.Errors, Report);

        var parser = new Parser(tokens);
        var statements = parser.Parse();
        errors.AddRangeAndReport(parser.Errors, Report);
        OnParserFinish?.Invoke(statements, parser.Errors);
        if (statements is null)
            return (errors.ToArray(), []);

        var resolver = new Resolver(Interpreter);
        resolver.Resolve(statements);
        errors.AddRangeAndReport(resolver.Errors, Report);
        if (resolver.Errors.Count > 0) 
            return (errors.ToArray(), []); 

        if (Dry) return (errors.ToArray(), []);
        // Running 
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