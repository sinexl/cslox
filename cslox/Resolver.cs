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
                ;
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
            case Return(var expression) returnExpr:
                if (_currentFunction == FunctionType.None)
                    Error(returnExpr.Location, "Cannot return from top-level code.");
                Resolve(expression);
                return;
            case Break: return;
            // 
            default:
            {
                byte staticAssert = Statement.InheritorsAmount == 10 ? 0 : -1;
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
                        if (value is false)
                            Error(r.Location, name, "Cannot read local variable in its own initializer.");
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
            Declare(param.Lexeme);
            Define(param.Lexeme);
        }

        Resolve(function.Body);
        ExitScope();
        _currentFunction = enclosing;
    }


    private void Define(string name)
    {
        if (_scopes.IsEmpty()) return;
        _scopes.Peek()[name] = true;
    }

    private void Declare(string name)
    {
        if (_scopes.IsEmpty()) return;

        var scope = _scopes.Peek();
        if (scope.ContainsKey(name))
        {
            // TODO: Report proper location.
            Error(new SourceLocation(), name, "Variable with this name already declared in this scope.");
        }

        scope[name] = false;
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

    private void Error(SourceLocation location, string message) => Errors.Add(new Error(location, message));

    private void Error(SourceLocation location, string name, string message) =>
        Errors.Add(new Error(location, $"{name}: {message}"));

    public void EnterScope() => _scopes.Push(new());

    public void ExitScope() => _scopes.Pop();

    private Stack<Dictionary<string, bool>> _scopes = new();

    public Resolver(Interpreter interpreter)
    {
        Interpreter = interpreter;
    }

    public List<Error> Errors { get; } = [];
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