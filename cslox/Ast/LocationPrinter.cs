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
                    sb[previousIndex - 1] = ' '; // remove the n
                    sb.Insert(previousIndex, $"{op.Type.Terminal()}\n");
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
                case Call(var callee, var arguments):
                {
                    Impl(callee, indent + 1);
                    foreach (var arg in arguments)
                    {
                        Impl(arg, indent + 2);
                    }

                    break;
                }

                case Lambda(var @params, var body):
                {
                    var paramsAsStr = string.Join(", ", @params);
                    sb[previousIndex - 1] = ' '; // remove the newline 
                    sb.Insert(previousIndex, $"lambda`{@params.Length}({paramsAsStr})\n");
                    break;
                }
                case Get(var obj, var name):
                {
                    sb[previousIndex - 1] = ' '; // remove the n
                    sb.Insert(previousIndex, $"`{name}`\n");
                    Impl(obj, indent + 1);
                    break;
                }
                case Set(var obj, var name, var value):
                {
                    sb[previousIndex - 1] = ' '; // remove the n
                    sb.Insert(previousIndex, $"`{name}`\n");
                    Impl(obj, indent + 1);
                    Impl(value, indent + 1);
                    break;
                }
                case This @this: break;
            }
        }

        byte staticAssert = Expression.InheritorsAmount == 25 ? 0 : -1;
        _ = staticAssert;

        Impl(expression, indentation);
        return sb.ToString();
    }

    public static void Test()
    {
        var tokens = new Lexer("name.Val.Rad = 15", "").Accumulate();
        var expr = new Parser(tokens).ParseExpression() ?? throw new ArgumentNullException();

        var self = new LocationPrinter();
        Console.WriteLine(self.Print(expr));
    }
}