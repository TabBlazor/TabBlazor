#nullable enable
using System.Collections;
using System.Reflection;
using System.Text.Json;

namespace TabBlazor;

/// <summary>
/// Builds <see cref="DataColumnInfo"/> from typed expressions: works out the column's stable id and the member
/// paths used to sort, filter and search. Used by <c>DataColumn</c>, and by tests and custom views that
/// need the same column descriptions without rendering.
/// </summary>
public static class DataColumnInfoBuilder
{
    /// <summary>
    /// Describes a column.
    /// </summary>
    /// <param name="value">The displayed value: a member path or any expression.</param>
    /// <param name="id">An explicit id; when null it is derived from <paramref name="value"/>.</param>
    /// <param name="sortBy">Sort by these members instead of the value (one member, or <c>new { a, b }</c> for several).</param>
    /// <param name="filterBy">Filter and search on these members instead of the value.</param>
    /// <param name="searchExpression">Custom in-memory search matching.</param>
    /// <param name="fallbackId">Used when no id is given and none can be derived (no value, or a value that uses no members).</param>
    public static DataColumnInfo Create<TItem, TValue>(
        Expression<Func<TItem, TValue>>? value,
        string? id = null,
        Expression<Func<TItem, object>>? sortBy = null,
        Expression<Func<TItem, object>>? filterBy = null,
        Expression<Func<TItem, string, bool>>? searchExpression = null,
        string? fallbackId = null) =>
        Create(typeof(TValue), value, id, sortBy, filterBy, searchExpression, fallbackId);

    /// <summary>Untyped variant of <see cref="Create{TItem,TValue}"/>.</summary>
    public static DataColumnInfo Create(Type valueType, LambdaExpression? value, string? id = null,
        LambdaExpression? sortBy = null, LambdaExpression? filterBy = null, LambdaExpression? searchExpression = null,
        string? fallbackId = null)
    {
        var valuePath = value == null ? null : GetMemberPath(value);
        var members = value == null ? [] : CollectMemberPaths(value);
        var isText = DataColumnInfo.KindOf(valueType) == DataValueKind.Text;

        var resolvedId = !string.IsNullOrWhiteSpace(id)
            ? id
            : valuePath ?? (members.Count > 0 ? string.Join("-", members) : fallbackId);
        if (string.IsNullOrWhiteSpace(resolvedId))
        {
            throw new InvalidOperationException(
                "A DataColumn needs an Id when its Value uses no members of the row (or when it has no Value).");
        }

        // Sort
        IReadOnlyList<LambdaExpression> sortExpressions;
        IReadOnlyList<string> sortPaths;
        if (sortBy != null)
        {
            sortExpressions = SplitParts(sortBy);
            sortPaths = sortExpressions.SelectMany(PathsOf).Distinct().ToList();
        }
        else
        {
            sortExpressions = value == null ? [] : [value];
            sortPaths = valuePath != null ? [valuePath] : isText ? members : [];
        }

        // Filter and search
        IReadOnlyList<LambdaExpression> filterExpressions;
        IReadOnlyList<string> filterPaths;
        FilterPathMode filterMode;
        Type filterType;
        if (filterBy != null)
        {
            filterExpressions = SplitParts(filterBy);
            filterPaths = filterExpressions.SelectMany(PathsOf).Distinct().ToList();
            if (filterExpressions.Count == 1)
            {
                filterMode = FilterPathMode.Exact;
                filterType = filterExpressions[0].ReturnType;
            }
            else
            {
                filterMode = FilterPathMode.Parts;
                filterType = typeof(string);
            }
        }
        else
        {
            filterExpressions = value == null ? [] : [value];
            filterType = valueType;
            if (valuePath != null)
            {
                filterPaths = [valuePath];
                filterMode = FilterPathMode.Exact;
            }
            else if (isText && members.Count > 0)
            {
                filterPaths = members;
                filterMode = FilterPathMode.Parts;
            }
            else
            {
                filterPaths = [];
                filterMode = FilterPathMode.Exact;
            }
        }

        return new DataColumnInfo(resolvedId, valueType, sortPaths, filterPaths, filterMode, value)
        {
            Kind = DataColumnInfo.KindOf(filterType),
            FilterValueType = filterType,
            SortExpressions = sortExpressions,
            FilterExpressions = filterExpressions,
            SearchExpression = searchExpression
        };
    }

    /// <summary>
    /// The member path of an expression that is a plain member chain from the parameter, in camelCase
    /// (<c>o =&gt; o.Customer.Name</c> → <c>customer.name</c>); otherwise null.
    /// </summary>
    public static string? GetMemberPath(LambdaExpression expression)
    {
        if (expression.Parameters.Count == 0)
        {
            return null;
        }

        var body = StripConvert(expression.Body);
        var chain = GetChain(body, expression.Parameters[0]);
        if (chain == null || chain.Count == 0)
        {
            return null;
        }

        var path = TruncatedPath(chain, expression.Parameters[0].Type, out var truncated);
        return truncated ? null : path;
    }

    /// <summary>
    /// The member paths an expression uses, in order of first use (<c>u =&gt; $"{u.FirstName} {u.LastName}"</c>
    /// → <c>firstName</c>, <c>lastName</c>). Members of simple values (<c>.Length</c>, <c>.Year</c>,
    /// <c>.Value</c>) and of collections are cut off, so <c>o.OrderDate.Year</c> uses <c>orderDate</c>.
    /// </summary>
    public static IReadOnlyList<string> CollectMemberPaths(LambdaExpression expression)
    {
        if (expression.Parameters.Count == 0)
        {
            return [];
        }

        var collector = new MemberCollector(expression.Parameters[0]);
        collector.Visit(expression.Body);
        return collector.Paths;
    }

    /// <summary>
    /// Splits <c>x =&gt; new { x.A, x.B }</c> into one lambda per member; any other expression is returned as is
    /// (without a boxing conversion).
    /// </summary>
    public static IReadOnlyList<LambdaExpression> SplitParts(LambdaExpression expression)
    {
        var parameter = expression.Parameters[0];
        var body = StripConvert(expression.Body);
        if (body is NewExpression { Arguments.Count: > 0 } newExpression && IsAnonymous(newExpression.Type))
        {
            return newExpression.Arguments.Select(a => Expression.Lambda(a, parameter)).ToList();
        }

        return [Expression.Lambda(body, parameter)];
    }

    /// <summary>Converts a member name to the camelCase used in paths and ids.</summary>
    public static string ToCamelCase(string name) => JsonNamingPolicy.CamelCase.ConvertName(name);

    private static IEnumerable<string> PathsOf(LambdaExpression part)
    {
        var path = GetMemberPath(part);
        return path != null ? [path] : CollectMemberPaths(part);
    }

    private static bool IsAnonymous(Type type) =>
        type.Name.Contains("AnonymousType", StringComparison.Ordinal) && type.Namespace == null;

    internal static Expression StripConvert(Expression expression)
    {
        while (expression is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } unary)
        {
            expression = unary.Operand;
        }

        return expression;
    }

    // Returns the members from the parameter outwards, or null when the expression is not a member chain rooted at the parameter.
    private static List<MemberInfo>? GetChain(Expression expression, ParameterExpression parameter)
    {
        var members = new List<MemberInfo>();
        var current = expression;
        while (true)
        {
            current = StripConvert(current);
            switch (current)
            {
                case MemberExpression { Expression: not null } member:
                    members.Add(member.Member);
                    current = member.Expression;
                    continue;
                case ParameterExpression p when p == parameter:
                    members.Reverse();
                    return members;
                default:
                    return null;
            }
        }
    }

    private static string TruncatedPath(List<MemberInfo> chain, Type rootType, out bool truncated)
    {
        var segments = new List<string>();
        var ownerType = rootType;
        truncated = false;
        foreach (var member in chain)
        {
            if (!IsComplex(ownerType))
            {
                truncated = true;
                break;
            }

            segments.Add(ToCamelCase(member.Name));
            ownerType = member switch
            {
                PropertyInfo property => property.PropertyType,
                FieldInfo field => field.FieldType,
                _ => typeof(object)
            };
        }

        return string.Join(".", segments);
    }

    // A type whose members are part of a path (an entity or DTO), as opposed to a simple value or a collection.
    private static bool IsComplex(Type type)
    {
        if (Nullable.GetUnderlyingType(type) != null || type.IsPrimitive || type.IsEnum || type == typeof(string) ||
            type == typeof(decimal) || type == typeof(Guid) || type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) || type == typeof(DateOnly) || type == typeof(TimeOnly) ||
            type == typeof(TimeSpan))
        {
            return false;
        }

        return !typeof(IEnumerable).IsAssignableFrom(type);
    }

    private sealed class MemberCollector(ParameterExpression parameter) : ExpressionVisitor
    {
        public List<string> Paths { get; } = [];

        protected override Expression VisitMember(MemberExpression node)
        {
            var chain = GetChain(node, parameter);
            if (chain == null)
            {
                return base.VisitMember(node);
            }

            var path = TruncatedPath(chain, parameter.Type, out _);
            if (path.Length > 0 && !Paths.Contains(path))
            {
                Paths.Add(path);
            }

            return node;
        }
    }
}
