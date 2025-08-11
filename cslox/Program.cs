using System.Diagnostics.Contracts;
using cslox;
using cslox.Ast;

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
        Prompt();
    }

    return 0;
}

return Main(args);

[Pure]
Error[] RunCode(string src, string? filePath = null)
{
    var interpreter = new Interpreter();
    var errors = new List<Error>();
    var lexer = new Lexer(src, filePath: filePath ?? "<REPL>");
    var tokens = lexer.Accumulate();
    errors.AddRange(lexer.Errors);
    var parser = new Parser(tokens);
    var expression = parser.ParseExpression();
    Console.Write(expression);
    // errors.AddRange();
    // TODO: Proper error handling
    if (expression is null)
    {
        Console.WriteLine("Error: Parse error occured.");
    }

    if (errors.Count == 0 && expression is not null)
    {
        var result = interpreter.Evaluate(expression);
        Console.WriteLine(result.LoxPrint());
    }

    Util.ReportAllErrorsIfSome(errors);
    return errors.ToArray();
}

void RunFile(string filePath)
{
    var src = File.ReadAllText(filePath);
    var errors = RunCode(src, filePath);
    var exit = Util.ReportAllErrorsIfSome(errors);
    if (exit)
    {
        return;
    }
}

void Prompt()
{
    Console.WriteLine("NOTE: Enter :quit or Press Ctrl+D to quit.");
    while (true)
    {
        Console.Write("> ");
        string? line = Console.ReadLine();
        if (line is null || line == ":quit")
            break;
        var errors = RunCode(line);
        var exit = Util.ReportAllErrorsIfSome(errors);
        if (errors.Length > 0)
        {
            Console.WriteLine($"Found {errors.Length} errors");
        }
    }
}