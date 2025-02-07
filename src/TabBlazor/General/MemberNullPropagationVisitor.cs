
namespace TabBlazor;

internal static class ExpressionExtensions
{
    internal static Expression PropagateNull(this Expression expression)
    {
        if (expression == null) { return null; }
        return new MemberNullPropagationVisitor().Visit(expression);
    }


    internal static Func<T> PropagateNull<T>(this Expression<Func<T>> expression)
    {
        if (expression == null) { return null; }
        var defaultValue = Expression.Constant(default(T));
        var body = expression.Body.PropagateNull();
        if (body.Type != typeof(T))
            body = Expression.Coalesce(body, defaultValue);
        return Expression.Lambda<Func<T>>(body, expression.Parameters)
            .Compile();
    }

}


//https://stackoverflow.com/questions/30488022/how-to-use-expression-tree-to-safely-access-path-of-nullable-objects
internal class MemberNullPropagationVisitor : ExpressionVisitor
{
    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression == null  || !IsNullable(node.Type)) //|| !IsNullable(node.Expression.Type)
            return base.VisitMember(node);

        var expression = base.Visit(node.Expression);
        var nullBaseExpression = Expression.Constant(null, expression.Type);
        var test = Expression.Equal(expression, nullBaseExpression);
        var memberAccess = Expression.MakeMemberAccess(expression, node.Member);
        var nullMemberExpression = Expression.Constant(null, node.Type);
        return Expression.Condition(test, nullMemberExpression, node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Object == null || !IsNullable(node.Object.Type))
            return base.VisitMethodCall(node);

        var expression = base.Visit(node.Object);
        var nullBaseExpression = Expression.Constant(null, expression.Type);
        var test = Expression.Equal(expression, nullBaseExpression);
        var memberAccess = Expression.Call(expression, node.Method);
        var nullMemberExpression = Expression.Constant(null, MakeNullable(node.Type));
        return Expression.Condition(test, nullMemberExpression, node);
    }

    private static Type MakeNullable(Type type)
    {
        if (IsNullable(type))
            return type;

        return typeof(Nullable<>).MakeGenericType(type);
    }

    private static bool IsNullable(Type type)
    {
        if (type.IsClass)
            return true;
        return type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }
}
