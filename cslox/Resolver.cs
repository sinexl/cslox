using System.Collections;
using System.Diagnostics;
using cslox.Ast.Generated;
using cslox.Runtime;

namespace cslox;

public class Resolver : IExpressionVisitor<Unit>, IStatementVisitor<Unit>
{
    Unit IExpressionVisitor<Unit>.Visit<TExpression>(TExpression expression)
    {
        Resolve(expression);
        return new Unit();
    }


    Unit IStatementVisitor<Unit>.Visit<TStatement>(TStatement statement)
    {
        Resolve(statement);
        return new Unit();
    }

    public void Resolve(IList<Statement> statements)
    {
        foreach (var statement in statements)
            Resolve(statement);
    }

    public void Resolve(Statement statement)
    {
        switch (statement)
        {
            case Block(var statements):
                EnterScope();
                Resolve(statements);
                ExitScope();
                return;
            case VarDeclaration(var name, var initializer):
                Declare(name);
                if (initializer is not null) Resolve(initializer);
                Define(name);
                return;
            case Function(var name, _, _) s:
                Declare(name);
                Define(name);
                ResolveFunction(s.GetInfo(), FunctionType.Function);
                return;
            case If(var condition, var thenBranch, var elseBranch):
                Resolve(condition);
                Resolve(thenBranch);
                if (elseBranch is not null) Resolve(elseBranch);
                return;
            case While(var condition, var body):
                Resolve(condition);
                Resolve(body);
                return;
            // base cases. 
            case ExpressionStatement(var expression):
                Resolve(expression);
                return;
            case Print(var expression):
                Resolve(expression);
                return;
            case Class(var name, var body):
                Declare(name); 
                Define(name);
                // Todo: resolve class body
                return;

            case Return(var expression) returnExpr:
                if (_currentFunction == FunctionType.None)
                    Error(new TopLevelReturn(returnExpr));
                Resolve(expression);
                return;
            case Break:
                return;
            // 
            default:
            {
                byte staticAssert = Statement.InheritorsAmount == 11 ? 0 : -1;
                _ = staticAssert;
                throw new UnreachableException("Not all cases are handled");
            }
        }
    }

    private void Resolve(Expression expression)
    {
        switch (expression)
        {
            case ReadVariable(var name) r:
                if (!_scopes.IsEmpty())
                {
                    var currentScope = _scopes.Peek();
                    if (currentScope.TryGetValue(name, out var value))
                        if (value.IsDefined is false)
                        {
                            Error(new ReadingFromInitializer(value.Name));
                        }
                        else
                            value.IsRead = true;
                }

                ResolveLocal(r, name);
                return;
            case Assign(var name, var value) a:
                Resolve(value);
                ResolveLocal(a, name);
                return;
            case Binary(var left, var right):
                Resolve(left);
                Resolve(right);
                return;
            case Call(var callee, var arguments):
                Resolve(callee);
                foreach (var argument in arguments) Resolve(argument);
                return;
            case Lambda s:
                ResolveFunction(s.GetInfo(), FunctionType.Function);
                return;
            // 
            case Grouping(var expr):
                Resolve(expr);
                return;
            case Unary (var expr, _):
                Resolve(expr);
                return;
            case Literal: return;
            case Sequence: throw new NotImplementedException("Sequences are not fully supported yet.");
            default:
                byte staticAssert = Expression.InheritorsAmount == 22 ? 0 : -1;
                _ = staticAssert;
                throw new UnreachableException("Not all cases are handled");
        }
    }

    private void ResolveFunction(LoxFunctionInfo function, FunctionType type)
    {
        FunctionType enclosing = _currentFunction;
        _currentFunction = type;
        EnterScope();
        foreach (var param in function.Params)
        {
            Declare(param);
            Define(param);
        }

        Resolve(function.Body);
        ExitScope();
        _currentFunction = enclosing;
    }


    private void Define(Identifier name)
    {
        if (_scopes.IsEmpty()) return;
        var scope = _scopes.Peek();
        if (!scope.TryGetValue(name, out var value)) throw new ArgumentException($"Variable {name} is not declared.");
        if (value.IsDefined) throw new ArgumentException($"Variable {name} is already defined.");
        value.IsDefined = true;
    }

    private void Declare(Identifier name)
    {
        if (_scopes.IsEmpty()) return;

        var scope = _scopes.Peek();
        if (scope.TryGetValue(name, out var existing))
            Error(new VariableRedefinition(existing.Name, name));

        scope[name] = new Variable(name) { IsDefined = false };
    }

    private void ResolveLocal(Expression expression, string name)
    {
        foreach (var (i, scope) in _scopes.EnumerateWithIndex())
            if (scope.ContainsKey(name))
            {
                Interpreter.Resolve(expression, _scopes.Count - i - 1);
                // Resolutions[expression] = _scopes.Count - 1 - i;
                return;
            }
    }


    public void EnterScope() => _scopes.Push(new());

    public void ExitScope()
    {
        var currentScope = _scopes.Pop();
        foreach (var (_, v) in currentScope)
            if (!v.IsRead)
                Warning(new UnusedVariable(v.Name));
    }

    private Stack<Dictionary<string, Variable>> _scopes = new();
    private void Error(AnalysisError error) => Errors.Add(error);
    private void Warning(AnalysisWarning warning) => Warnings.Add(warning);

    public Resolver(Interpreter interpreter)
    {
        Interpreter = interpreter;
    }

    public List<Error> Errors { get; } = [];
    public List<Warning> Warnings { get; } = [];
    private FunctionType _currentFunction = FunctionType.None;
    public Interpreter Interpreter { get; set; }
}

public static class ResolverExtensions
{
    public static bool IsEmpty(this ICollection collection) => collection.Count == 0;

    public static IEnumerable<(int Index, T Value)> EnumerateWithIndex<T>(this Stack<T> stack)
    {
        int index = stack.Count - 1; // top = Count - 1
        foreach (var value in stack)
        {
            var ret = (index, value);
            index--;
            yield return ret;
        }
    }
}

public enum FunctionType
{
    None,
    Function,
}

public class Variable
{
    public Identifier Name { get; }
    public required bool IsDefined { get; set; }
    public bool IsRead { get; set; } = false;

    public Variable(Identifier name)
    {
        Name = name;
    }
}

public abstract class AnalysisError(SourceLocation location, string message, string? note = null)
    : Error(location, message, note);

public class VariableRedefinition : AnalysisError
{
    public VariableRedefinition(Identifier firstDefined, Identifier redefinition) :
        base(redefinition.Location, $"Variable `{redefinition.Id}` is already defined.",
            note: $"First definition happens here: \n\t\t{firstDefined}")
    {
        if (redefinition.Id != firstDefined.Id) throw new ArgumentException("Ids should be the same.");
        FirstDefined = firstDefined;
        Redefined = redefinition;
    }

    public Identifier FirstDefined { get; init; }
    public Identifier Redefined { get; init; }
}

public class TopLevelReturn(Return expr) : AnalysisError(expr.Location, "Cannot return from top-level code")
{
    public Return Expr { get; init; } = expr;
}

public class ReadingFromInitializer(Identifier id)
    : AnalysisError(id.Location, $"Cannot read variable `{id.Id}` from it's initializer")
{
    public Identifier Id { get; init; } = id;
}

public class AnalysisWarning(SourceLocation location, string message, string? note = null)
    : Warning(location, message, note);

public class UnusedVariable(Identifier variable) : AnalysisWarning(variable.Location,
    $"Variable `{variable.Id}` is unused",
    $"Definition happens here: \n\t\t{variable.Location}")
{
    public Identifier Variable { get; init; } = variable;
}