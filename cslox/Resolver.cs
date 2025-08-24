using System.Collections;
using System.Diagnostics;
using cslox.Ast.Generated;
using cslox.Runtime;

namespace cslox;

public class Resolver : IExpressionVisitor<Unit>, IStatementVisitor<Unit>
{
    private readonly Stack<Dictionary<string, Variable>> _scopes = new();
    private ClassType _currentClass = ClassType.None;
    private FunctionType _currentFunction = FunctionType.None;

    public Resolver(Interpreter interpreter)
    {
        Interpreter = interpreter;
    }

    public List<Error> Errors { get; } = [];
    public List<Warning> Warnings { get; } = [];
    public Dictionary<string, Variable> CurrentScope => _scopes.Peek();
    public Interpreter Interpreter { get; set; }

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
            case Class(var name, var superclass, var body):
                var enclosingClass = _currentClass;
                _currentClass = ClassType.Class;
                Declare(name);
                Define(name);
                if (superclass is not null)
                {
                    if (name.Id == superclass.Name.Id)
                    {
                        Error(new SelfInheritanceError(superclass.Name));
                        return;
                    }

                    Resolve(superclass);

                    EnterScope();
                    CurrentScope["super"] = new Variable(new Identifier("super", superclass.Location))
                        { IsDefined = true };
                }

                EnterScope();
                CurrentScope["this"] = new Variable(new Identifier("this", name.Location))
                {
                    IsDefined = true
                };
                foreach (var method in body)
                {
                    FunctionType declaration = FunctionType.Method;
                    if (method.Name == "init") declaration = FunctionType.Initializer;
                    ResolveFunction(method.GetInfo(), declaration);
                }

                ExitScope();
                if (superclass is not null) ExitScope();
                _currentClass = enclosingClass;
                return;
            case Return(var ret) returnExpr:
                if (_currentFunction == FunctionType.None)
                    Error(new TopLevelReturn(returnExpr));
                if (ret is not null)
                {
                    if (_currentFunction == FunctionType.Initializer)
                        Error(new ReturnFromInitializer(returnExpr));

                    Resolve(ret);
                }

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
                    if (CurrentScope.TryGetValue(name, out var value))
                        if (value.IsDefined is false)
                            Error(new ReadingFromInitializer(value.Name));
                        else
                            value.IsRead = true;

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
            case Get(var obj, _):
                Resolve(obj);
                // Names are not resolved since they're dynamic
                return;
            case Set(var obj, _, var value):
                Resolve(value);
                Resolve(obj);
                // Names are not resolved since they're dynamic
                break;
            // 
            case Grouping(var expr):
                Resolve(expr);
                return;
            case Unary (var expr, _):
                Resolve(expr);
                return;
            case Literal: return;
            case This @this:
                if (_currentClass == ClassType.None)
                {
                    Error(new UsingThisOutsideOfMethod(@this));
                    return;
                }

                ResolveLocal(@this, "this");
                return;
            case Super super:
                ResolveLocal(super, "super");
                return;
            case Sequence: throw new NotImplementedException("Sequences are not fully supported yet.");
            default:
                byte staticAssert = Expression.InheritorsAmount == 26 ? 0 : -1;
                _ = staticAssert;
                throw new UnreachableException("Not all cases are handled");
        }
    }


    // private void SetRead(Identifier name, bool read)
    // {
    //     var firstDefined = CurrentScope;
    //     if (firstDefined.TryGetValue(name, out var value)) value.IsRead = read; 
    //     else throw new ArgumentException($"Variable {name} is not defined."); 
    //     foreach (var scope in _scopes)
    //     {
    //         if (scope.ContainsKey(name))
    //             firstDefined = scope; 
    //     }
    //
    //     firstDefined[name].IsRead = read; 
    // }

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
        if (!CurrentScope.TryGetValue(name, out var value))
            throw new ArgumentException($"Variable {name} is not declared.");
        if (value.IsDefined) throw new ArgumentException($"Variable {name} is already defined.");
        value.IsDefined = true;
    }

    private void Declare(Identifier name)
    {
        if (_scopes.IsEmpty()) return;

        if (CurrentScope.TryGetValue(name, out var existing))
            Error(new VariableRedefinition(existing.Name, name));

        CurrentScope[name] = new Variable(name) { IsDefined = false };
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


    public void EnterScope()
    {
        _scopes.Push(new Dictionary<string, Variable>());
    }

    public void ExitScope()
    {
        _ = _scopes.Pop();
        // foreach (var (_, v) in currentScope)
        //     if (!v.IsRead)
        //         Warning(new UnusedVariable(v.Name));
    }

    private void Error(AnalysisError error)
    {
        Errors.Add(error);
    }

    private void Warning(AnalysisWarning warning)
    {
        Warnings.Add(warning);
    }
}

public class SelfInheritanceError(Identifier name) : AnalysisError(name.Location, "Classes cannot inherit themselves")
{
    public Identifier Name { get; init; } = name;
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
    Method,
    Initializer
}

public enum ClassType
{
    None,
    Class
}

public class Variable
{
    public Variable(Identifier name)
    {
        Name = name;
    }

    public Identifier Name { get; }
    public required bool IsDefined { get; set; }
    public bool IsRead { get; set; }
}

public abstract class AnalysisError(SourceLocation location, string message, string? note = null)
    : Error(location, message, note);

public class VariableRedefinition : AnalysisError
{
    public VariableRedefinition(Identifier firstDefined, Identifier redefinition) :
        base(redefinition.Location, $"Variable `{redefinition.Id}` is already defined.",
            $"First definition happens here: \n\t\t{firstDefined}")
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

public class ReturnFromInitializer(Return expr) : AnalysisError(expr.Location, "Cannot return from initializer.")
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

public class UsingThisOutsideOfMethod(This @this)
    : AnalysisError(@this.Location, "Cannot use `this` outside of method")
{
    public This This { get; init; } = @this;
}