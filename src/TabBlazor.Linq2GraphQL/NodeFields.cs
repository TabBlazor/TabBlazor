using System.Linq.Expressions;
using System.Reflection;
using Linq2GraphQL.Client;

namespace TabBlazor.Linq2GraphQL;

/// <summary>
/// The fields of a row type that a request's columns read: the members their value, sort and filter expressions use
/// (the key is one of the request's columns when it is a member path). Each field is a chain of
/// <see cref="GraphQLMemberAttribute"/> properties from the row, such as <c>Customer.Name</c>.
/// </summary>
internal static class NodeFields
{
    /// <summary>
    /// The member chains the columns read, or null when a column uses the row in a way the chains cannot describe
    /// (the row passed to a method, for example), so the caller must fetch every field.
    /// </summary>
    public static IReadOnlyList<PropertyInfo[]>? Collect(Type rowType, IReadOnlyList<DataColumnInfo> columns)
    {
        var collector = new Collector();
        foreach (var column in columns)
        {
            IEnumerable<LambdaExpression?> expressions = [column.ValueExpression, .. column.SortExpressions, .. column.FilterExpressions];
            foreach (var expression in expressions)
            {
                if (expression is { Parameters.Count: 1 } lambda && lambda.Parameters[0].Type == rowType)
                {
                    collector.Read(lambda);
                }
            }
        }

        return collector.Complete ? collector.Fields : null;
    }

    private sealed class Collector : ExpressionVisitor
    {
        private readonly HashSet<string> keys = new(StringComparer.Ordinal);
        private ParameterExpression? row;

        public List<PropertyInfo[]> Fields { get; } = [];

        public bool Complete { get; private set; } = true;

        public void Read(LambdaExpression lambda)
        {
            row = lambda.Parameters[0];
            Visit(lambda.Body);
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var chain = new List<MemberInfo>();
            Expression? current = node;
            while (current is MemberExpression member)
            {
                chain.Insert(0, member.Member);
                current = StripConvert(member.Expression);
            }

            if (current != row)
            {
                // Not read from the row (a captured value, or a lambda parameter of a nested query).
                return base.VisitMember(node);
            }

            // GraphQL fields up to the first member that is not one (string.Length, Nullable.Value).
            var fields = chain.TakeWhile(m => m is PropertyInfo p && p.GetCustomAttribute<GraphQLMemberAttribute>() != null)
                .Cast<PropertyInfo>()
                .ToArray();
            if (fields.Length == 0)
            {
                Complete = false;
            }
            else if (keys.Add(string.Join(".", fields.Select(f => f.Name))))
            {
                Fields.Add(fields);
            }

            return node;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            // The row itself is used, not one of its members (passed to a method, or displayed whole).
            if (node == row)
            {
                Complete = false;
            }

            return node;
        }

        private static Expression? StripConvert(Expression? expression)
        {
            while (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked or ExpressionType.TypeAs } unary)
            {
                expression = unary.Operand;
            }

            return expression;
        }
    }
}
