#nullable enable
namespace TabBlazor;

/// <summary>
/// Null checks for a plain member chain such as <c>o =&gt; o.Customer.Address.City</c>: true when every object on
/// the way is there, so getters can skip a row instead of catching a <see cref="NullReferenceException"/> for it.
/// </summary>
internal static class NullGuard
{
    /// <summary>The check for <paramref name="expression"/>, or null when it is not a plain member chain.</summary>
    public static Func<TItem, bool>? ForMemberChain<TItem>(LambdaExpression expression)
    {
        if (expression.Parameters.Count != 1 || expression.Parameters[0].Type != typeof(TItem))
        {
            return null;
        }

        var body = expression.Body;
        while (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } convert)
        {
            body = convert.Operand;
        }

        var owners = new List<Expression>();
        while (body is MemberExpression { Expression: { } owner })
        {
            if (!owner.Type.IsValueType)
            {
                owners.Add(owner);
            }

            body = owner;
        }

        if (body != expression.Parameters[0])
        {
            return null;
        }

        if (owners.Count == 0)
        {
            return static _ => true;
        }

        // Outermost first, so the && stops at the first missing object.
        owners.Reverse();
        var check = owners
            .Select(o => (Expression)Expression.NotEqual(o, Expression.Constant(null, o.Type)))
            .Aggregate(Expression.AndAlso);
        return Expression.Lambda<Func<TItem, bool>>(check, expression.Parameters).Compile();
    }
}
