using System.Diagnostics;
using System.Text;
using cslox.Ast.Generated;

namespace cslox.Ast;

public class PrefixPrinter : IExpressionVisitor<string>
{
    public string Visit<TExpression>(TExpression expression) where TExpression : Expression
    {
        switch (expression)
        {
            case Binary (var left, var right) a:
            {
                return a switch
                {
                    Addition => Parenthesise("+", left, right),
                    Multiplication => Parenthesise("*", left, right),
                    Subtraction => Parenthesise("-", left, right),
                    Division => Parenthesise("/", left, right),
                    Equality => Parenthesise("==", left, right),
                    Inequality => Parenthesise("!=", left, right),
                    Greater => Parenthesise(">", left, right),
                    GreaterEqual => Parenthesise(">=", left, right),
                    Less => Parenthesise("<", left, right),
                    LessEqual => Parenthesise("<=", left, right),
                    LogicalAnd => Parenthesise("and", left, right),
                    LogicalOr => Parenthesise("or", left, right),
                    _ => throw new UnreachableException("Not all cases are handled")
                };
            }
            case Assign(var name, var expr):
                return Parenthesise(name, expr);
            case Literal (var value):
            {
                return value switch
                {
                    string s => $"\"{s}\"",
                    null => "nil",
                    bool b => b
                        ? "true"
                        : "false", // for some reason bool.ToString returns Capitalized value (True, False)
                    _ => value.ToString() ?? throw new UnreachableException("should never be null")
                };
            }
            case Grouping(var inner):
                return Parenthesise("group", inner);
            case Unary(var inner, var op):
                return Parenthesise(op.Lexeme, inner);
            case Sequence(var expressions): return Sequence("sequence", expressions);
            case Lambda(var @params, _):
                string paramsAsStr = string.Join(", ", @params);
                return Sequence($"lambda`{@params.Length}<{paramsAsStr}>",
                    []); // TODO: Add support for statements in PrefixPrinter
            case ReadVariable(var name): return $"{name}.*";
            case Get(var obj, var name): return Parenthesise($"get.{name}", obj);
            case Set(var obj, var name, var value): return Parenthesise($"set.{name}", obj, value);

            case Call(_, var arguments):
                return Parenthesise("call", arguments);
            case This: return "this";
        }

        // This is how you do static assertions in this language. 
        // Welcome to C# 
        byte staticAssert = Expression.InheritorsAmount == 25 ? 0 : -1;
        _ = staticAssert;

        throw new UnreachableException("Not all cases are handled");
    }

    public string Print(Expression expression) => expression.Accept(this);

    private string Sequence(string name, Expression[] expressions) => Parenthesise(name, expressions);

    private string Parenthesise(string name, params IEnumerable<Expression> expressions)
    {
        expressions = expressions.ToArray();
        var sb = new StringBuilder();
        sb.Append("(")
            .Append(name)
            .Append(" ");
        foreach (var (index, expression) in expressions.Index())
        {
            sb.Append(expression.Accept(this));
            if (index != expressions.Count() - 1) sb.Append(" ");
        }

        sb.Append(")");
        return sb.ToString();
    }

    public static void Test()
    {
        var self = new PrefixPrinter();
        var expression = new Subtraction(
            new Literal(10) { Location = new SourceLocation() },
            new Literal(12) { Location = new SourceLocation() }
        )
        {
            Location = new SourceLocation()
        };

        Console.WriteLine(self.Print(expression));
    }
}