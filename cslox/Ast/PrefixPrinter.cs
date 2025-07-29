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
            case Binary (var left, var right, var op):
                return Parenthesise(op.Lexeme, left, right);
            case Literal (var value):
            {
                if (value is null) return "nil";
                return value.ToString() ?? throw new UnreachableException("should never be null");
            }
            case Grouping(var inner):
                return Parenthesise("group", inner);
            case Unary(var inner, var op):
                return Parenthesise(op.Lexeme, inner);
        }

        throw new UnreachableException("Not all cases are covered for some reason");
    }

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
        var expression = new Binary(
            new Literal(10), 
            new Literal(12), 
            new Token(TokenType.Minus, "-", null, new SourceLocation()) 
        );

        Console.WriteLine(self.Print(expression));
    }
}