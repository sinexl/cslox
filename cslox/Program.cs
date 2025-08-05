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
    Console.WriteLine($"src: {src}");
    var lexer = new Lexer(src, filePath: filePath ?? "<REPL>");
    var printer = new PrefixPrinter(); 
    var tokens = lexer.Accumulate();
    var parser = new Parser(tokens); 
    if (lexer.Errors.Count > 0) return lexer.Errors.ToArray();
    var expression = parser.ParseExpression();
    // TODO: Proper error handling
    if (expression is null)
    {
        Console.WriteLine("Parse error occured");
        Environment.Exit(65);
    } 


    Console.WriteLine(printer.Print(expression));
    return [];
}

void RunFile(string filePath)
{
    var src = File.ReadAllText(filePath);
    var errors = RunCode(src, filePath);
    var exit = ReportAllErrorsIfSome(errors);
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
        var exit = ReportAllErrorsIfSome(errors);
        Console.WriteLine($"Found {errors.Length} errors");
    }
}


bool ReportAllErrorsIfSome(Error[] errors1)
{
    throw new NotImplementedException();
}
