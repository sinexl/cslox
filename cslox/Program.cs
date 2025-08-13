using cslox;
using cslox.Ast.Generated;

int Main(string[] args)
{
    if (args.Length > 1)
    {
        Console.WriteLine("Usage: cslox <file>. Currently only single file is supported");
        return -1;
    }

    if (args.Length == 1)
    {
        RunFile(args[0]);
    }
    else
    {
        Repl();
    }

    return 0;
}

return Main(args);


void RunFile(string filePath)
{
    var runner = new Runner(filePath) { AllowRedefinition = false };
    var src = File.ReadAllText(filePath);
    var errors = runner.Run(src);
    if (errors.Any())
    {
        return;
    }
}

void Repl()
{
    bool enableTokenDebugging = false;
    bool enableAstDebugging = false;
    bool dry = false;

    Action<IList<Token>, IList<Error>> debugTokens =
        (tokens, _) => { tokens.ForEach(Console.WriteLine); };

    Action<IList<Statement>?, IList<Error>> debugAst =
        (list, _) => { list?.ForEach(Console.WriteLine); };


    var runner = new Runner("<REPL>") { AllowRedefinition = true };
    Console.WriteLine("NOTE: Enter :quit or Press Ctrl+D to quit.");
    bool exit = false;
    while (!exit)
    {
        Console.Write("> ");
        string? line = Console.ReadLine();
        switch (line?.Trim())
        {
            case null:
            case ":quit":
                exit = true;
                continue;
            case ":dry":
                dry = !dry;
                runner.Dry = dry;
                continue;
            case ":clear":
                Console.Clear();
                continue;
            case ":toggleTokens":
                if (enableTokenDebugging) runner.OnTokenizerFinish -= debugTokens;
                else runner.OnTokenizerFinish += debugTokens;
                enableTokenDebugging = !enableTokenDebugging;
                continue;
            case ":toggleAst":
                if (enableAstDebugging) runner.OnParserFinish -= debugAst;
                else runner.OnParserFinish += debugAst;
                enableAstDebugging = !enableAstDebugging;
                continue;
        }


        // Running
        var (errors, exceptions) = runner.Run(line);
        if (errors.Length > 0)
        {
            foreach (var error in errors)
                Console.WriteLine(error);
            Console.WriteLine($"Found {errors.Length} errors.");
        }

        if (exceptions.Length > 0)
        {
            foreach (var exception in exceptions)
                Console.WriteLine(exception.Message);

            Console.WriteLine($"{exceptions.Length} exceptions were thrown.");
        }
    }
}