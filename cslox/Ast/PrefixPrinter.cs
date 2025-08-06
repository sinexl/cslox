using System.Diagnostics;
using System.Text;
using cslox.Ast.Generated;

namespace cslox.Ast;

public class PrefixPrinter : IExpressionVisitor<string>
{
    public string Print(Expression expression)
    {
        return expression.Accept(this);
    }

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
                    _ => throw new UnreachableException("Not all cases are handled")
                };
            }
            case Literal (var value):
            {
                if (value is null) return "nil";
                return value.ToString() ?? throw new UnreachableException("should never be null");
            }
            case Grouping(var inner):
                return Parenthesise("group", inner);
            case Unary(var inner, var op):
                return Parenthesise(op.Lexeme, inner);
            case Sequence(var expressions): return Sequence("sequence", expressions);
        }

        // This is how you do static assertions in this language. 
        // Welcome to C# 
        byte staticAssert = Expression.InheritorsAmount == 16 ? 0 : -1;
        _ = staticAssert; 
        
        throw new UnreachableException("Not all cases are handled"); 
    }

    private string Sequence(string name, Expression[] expressions)
        => $"({Parenthesise(name, expressions)})";

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
            new Literal(10),
            new Literal(12)
        );

        Console.WriteLine(self.Print(expression));
    }
}