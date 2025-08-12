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

void Prompt()
{
    var runner = new Runner("<REPL>") { AllowRedefinition = true };
    Console.WriteLine("NOTE: Enter :quit or Press Ctrl+D to quit.");
    while (true)
    {
        Console.Write("> ");
        string? line = Console.ReadLine();
        if (line is null || line == ":quit")
            break;

        var (errors, exceptions) = runner.Run(line);
        if (errors.Length > 0)
            Console.WriteLine($"Found {errors.Length} errors.");
        if (exceptions.Length > 0)
            Console.WriteLine($"{exceptions.Length} exceptions were thrown.");
    }
}