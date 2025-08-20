using cslox.Ast.Generated;
using cslox.Runtime;

namespace cslox;

public class Runner
{
    // Events 
    public delegate void TokenHandler(Token[] tokens, IList<Error> errors);

    public delegate void ParserHandler(Statement[]? statements, IList<Error> errors);

    public delegate void ResolverHandler(Dictionary<Expression, int> locals, IList<Error> errors,
        IList<Warning> warnings);

    public event TokenHandler? OnTokenizerFinish;

    public event ParserHandler? OnParserFinish;
    public event ResolverHandler? OnResolverFinish;

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
        List<Warning> warnings = [];

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
        warnings.AddRangeAndReport(resolver.Warnings, Report);
        OnResolverFinish?.Invoke(resolver.Interpreter.Locals, resolver.Errors, resolver.Warnings);
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
    public static void AddRangeAndReport<T>(this List<T> items, IList<T> itemsToAdd, bool doReport)
        where T : WarningOrError
    {
        items.AddRange(itemsToAdd);
        if (!doReport) return;
        foreach (var error in itemsToAdd)
        {
            Console.Error.WriteLine(error);
        }
    }

    public static bool Any(this (IList<Error>, IList<LoxRuntimeException>) errors)
    {
        return errors.Item1.Any() || errors.Item2.Any();
    }
}