using cslox.Ast.Generated;

namespace cslox.Runtime;

// Class that stores common information from Expression.Lambda and Statement.Function 
public class LoxFunctionInfo
{
    public string? Name { get; init; }
    public Token[] Params { get; init; }
    public Statement[] Body { get; init; }

    public LoxFunctionInfo(string? name, Token[] parameters, Statement[] body)
    {
        Name = name;
        Params = parameters;
        Body = body;
    }

    public static LoxFunctionInfo FromFunction(Function function) => new(function.Name, function.Params, function.Body);
    public static LoxFunctionInfo FromLambda(Lambda lambda) => new(null, lambda.Params, lambda.Body);
}

public static class LoxFunctionInfoExtensions
{
    public static LoxFunctionInfo GetInfo(this Function function) => LoxFunctionInfo.FromFunction(function);
    public static LoxFunctionInfo GetInfo(this Lambda lambda) => LoxFunctionInfo.FromLambda(lambda);
}