using System.Text;
using cslox.Ast.Generated;

namespace cslox.Ast;

public class LocationPrinter : IExpressionVisitor<string>
{
    public string Visit<TExpression>(TExpression expression) where TExpression : Expression
    {
        return Print(expression);
    }

    public string Print(Expression expression, int indentation = 0)
    {
        const int spacesPerTab = 4;
        var sb = new StringBuilder();
        int previousIndex;

        void Impl(Expression expr, int indent)
        {
            string tabulation = new(' ', indent * spacesPerTab);
            sb.Append($"{expr.Location}:{tabulation} {expr.GetType().Name}\n");
            previousIndex = sb.Length;
            switch (expr)
            {
                case Grouping(var inner):
                    Impl(inner, indent + 1);
                    break;
                case Literal (var value):
                    sb[previousIndex - 1] = ' '; // remove the newline 
                    sb.Insert(previousIndex, $"{value}\n");
                    break;
                case ReadVariable (var name):
                    sb[previousIndex - 1] = ' '; // remove the newline 
                    sb.Insert(previousIndex, $"{name}\n");
                    break;
                case Assign (var name, var e):
                    sb[previousIndex - 1] = ' '; // remove the newline 
                    sb.Insert(previousIndex, $"{name}\n");
                    Impl(e, indent + 1);
                    break;
                case Unary(var inner, var op):
                    Impl(inner, indent + 1);
                    break;
                case Sequence(var expressions):
                    foreach (var e in expressions)
                    {
                        Impl(e, indent + 1);
                    }

                    break;
                case Binary (var left, var right):
                    Impl(left, indent + 1);
                    Impl(right, indent + 1);
                    break;
            }
        }

        byte staticAssert = Expression.InheritorsAmount == 20 ? 0 : -1;
        _ = staticAssert;

        Impl(expression, indentation);
        return sb.ToString();
    }
}