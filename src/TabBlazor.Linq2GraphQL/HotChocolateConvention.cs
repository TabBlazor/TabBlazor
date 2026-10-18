using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;

namespace TabBlazor.Linq2GraphQL;

/// <summary>
/// HotChocolate's filtering and sorting conventions (<c>[UseFiltering]</c>, <c>[UseSorting]</c>): operator names,
/// <c>and</c>/<c>or</c> lists, <c>eq: null</c> for empty values and a list of sort inputs with one field each.
/// Operator names can be overridden one by one, for example <c>new HotChocolateConvention { Contains = "icontains" }</c>.
/// </summary>
public class HotChocolateConvention : IGraphQLConvention
{
    /// <summary>Returned by <see cref="BuildFilter"/> when no row can match; the source then skips the request.</summary>
    public static readonly object MatchesNothing = new();

    public static HotChocolateConvention Default { get; } = new();

    public string Eq { get; init; } = "eq";
    public string Neq { get; init; } = "neq";
    public string Contains { get; init; } = "contains";
    public string StartsWith { get; init; } = "startsWith";
    public string EndsWith { get; init; } = "endsWith";
    public string Gt { get; init; } = "gt";
    public string Gte { get; init; } = "gte";
    public string Lt { get; init; } = "lt";
    public string Lte { get; init; } = "lte";
    public string In { get; init; } = "in";
    public string And { get; init; } = "and";
    public string Or { get; init; } = "or";
    public string Ascending { get; init; } = "ASC";
    public string Descending { get; init; } = "DESC";

    public virtual FieldCapabilities Describe(Type filterType, Type sortType, string path)
    {
        var sortable = SortLeaf(sortType, path) != null;
        var leaf = FilterLeaf(filterType, path);
        if (leaf == null)
        {
            return new FieldCapabilities(sortable, [], null);
        }

        var operators = GraphQLMembers.Of(leaf).Keys.Where(k => k != And && k != Or).ToList();
        var valueType = GraphQLMembers.Find(leaf, Eq)?.PropertyType;
        return new FieldCapabilities(sortable, operators, valueType);
    }

    public virtual object? BuildFilter(Type filterType, IReadOnlyList<FilterSpec> filters, SearchSpec? search, FieldPaths paths)
    {
        var builder = new InputBuilder(filterType, this);
        foreach (var filter in filters)
        {
            AddFilter(builder, filter, paths.Get(filter.Column));
        }

        if (search is { Terms.Count: > 0 })
        {
            AddSearch(builder, search, paths);
        }

        return builder.MatchesNothing ? MatchesNothing : builder.Build();
    }

    /// <summary>Adds one column filter. Several paths (Parts) match each word against one of them.</summary>
    protected virtual void AddFilter(InputBuilder builder, FilterSpec filter, GraphQLColumn column)
    {
        if (column.FilterPaths.Count == 0)
        {
            throw new InvalidOperationException($"Column '{column.Id}' has no GraphQL path to filter on.");
        }

        if (column.FilterMode == FilterPathMode.Parts && column.FilterPaths.Count > 1)
        {
            if (filter.Operator is FilterOperator.IsNull or FilterOperator.IsNotNull)
            {
                var checks = column.FilterPaths.Select(p => builder.Condition(p, (filter.Operator == FilterOperator.IsNull ? Eq : Neq, null)));
                builder.Add(filter.Operator == FilterOperator.IsNull ? builder.AllOf(checks) : builder.AnyOf(checks));
                return;
            }

            var op = filter.Operator == FilterOperator.StartsWith ? StartsWith : Contains;
            foreach (var word in SearchSpec.ParseTerms(Convert.ToString(filter.Value, CultureInfo.InvariantCulture) ?? string.Empty, int.MaxValue))
            {
                builder.Add(builder.AnyOf(column.FilterPaths.Select(p => builder.Condition(p, (op, word)))));
            }

            return;
        }

        var path = column.FilterPaths[0];
        builder.Add(filter.Operator switch
        {
            FilterOperator.Equals => builder.Condition(path, (Eq, filter.Value)),
            FilterOperator.NotEquals => builder.Condition(path, (Neq, filter.Value)),
            FilterOperator.Contains => builder.Condition(path, (Contains, filter.Value)),
            FilterOperator.StartsWith => builder.Condition(path, (StartsWith, filter.Value)),
            FilterOperator.EndsWith => builder.Condition(path, (EndsWith, filter.Value)),
            FilterOperator.GreaterThan => builder.Condition(path, (Gt, filter.Value)),
            FilterOperator.GreaterThanOrEqual => builder.Condition(path, (Gte, filter.Value)),
            FilterOperator.LessThan => builder.Condition(path, (Lt, filter.Value)),
            FilterOperator.LessThanOrEqual => builder.Condition(path, (Lte, filter.Value)),
            FilterOperator.Between => BetweenCondition(builder, path, filter),
            FilterOperator.In => builder.Condition(path, (In, DataFilters.AsList(filter.Value))),
            FilterOperator.IsNull => builder.Condition(path, (Eq, null)),
            _ => builder.Condition(path, (Neq, null))
        });
    }

    private object BetweenCondition(InputBuilder builder, string path, FilterSpec filter)
    {
        var operations = new List<(string, object?)>();
        if (filter.Value != null)
        {
            operations.Add((Gte, filter.Value));
        }

        if (filter.Value2 != null)
        {
            operations.Add((Lte, filter.Value2));
        }

        return builder.Condition(path, operations.ToArray());
    }

    /// <summary>
    /// Search: <c>and</c> over the terms, each an <c>or</c> across the searchable columns' paths. Text uses
    /// <c>contains</c>, numbers and Guids <c>eq</c> (when the term parses), enums <c>in</c> of the values whose name
    /// contains the term; dates and bools are not searched.
    /// </summary>
    protected virtual void AddSearch(InputBuilder builder, SearchSpec search, FieldPaths paths)
    {
        var columns = search.Columns.Select(paths.Get).ToList();
        foreach (var term in search.Terms)
        {
            var alternatives = new List<object>();
            foreach (var column in columns)
            {
                foreach (var path in column.FilterPaths)
                {
                    if (SearchCondition(builder, column, path, term) is { } condition)
                    {
                        alternatives.Add(condition);
                    }
                }
            }

            if (alternatives.Count == 0)
            {
                builder.MarkMatchesNothing();
                return;
            }

            builder.Add(builder.AnyOf(alternatives));
        }
    }

    protected virtual object? SearchCondition(InputBuilder builder, GraphQLColumn column, string path, string term)
    {
        var valueType = builder.Describe(path).ValueType;
        if (valueType == null)
        {
            return null;
        }

        var kind = column.FilterMode == FilterPathMode.Parts ? DataValueKind.Text : DataColumnInfo.KindOf(valueType);
        switch (kind)
        {
            case DataValueKind.Text:
                return builder.Condition(path, (Contains, term));
            case DataValueKind.Number:
                if (!decimal.TryParse(term, NumberStyles.Number, CultureInfo.CurrentCulture, out var number) &&
                    !decimal.TryParse(term, NumberStyles.Number, CultureInfo.InvariantCulture, out number))
                {
                    return null;
                }

                try
                {
                    return builder.Condition(path, (Eq, number));
                }
                catch (OverflowException)
                {
                    return null;
                }
            case DataValueKind.Guid:
                return Guid.TryParse(term, out var guid) ? builder.Condition(path, (Eq, guid)) : null;
            case DataValueKind.Enum:
                var enumType = Nullable.GetUnderlyingType(valueType) ?? valueType;
                var matches = Enum.GetValues(enumType).Cast<Enum>()
                    .Where(v => EnumDisplay.GetName(v).Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
                                v.ToString().Contains(term, StringComparison.CurrentCultureIgnoreCase))
                    .Cast<object>()
                    .ToList();
                return matches.Count == 0 ? null : builder.Condition(path, (In, matches));
            default:
                return null;
        }
    }

    public virtual object? BuildSort(Type sortType, IReadOnlyList<SortSpec> sorts, FieldPaths paths)
    {
        if (sorts.Count == 0)
        {
            return null;
        }

        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(sortType))!;
        foreach (var sort in sorts)
        {
            var column = paths.Get(sort.Column);
            if (column.SortPaths.Count == 0)
            {
                throw new InvalidOperationException($"Column '{column.Id}' has no GraphQL path to sort by.");
            }

            foreach (var path in column.SortPaths)
            {
                list.Add(SortItem(sortType, path, sort.Descending));
            }
        }

        return list;
    }

    /// <summary>One sort input with the direction set at the end of <paramref name="path"/>.</summary>
    protected virtual object SortItem(Type sortType, string path, bool descending)
    {
        var root = Activator.CreateInstance(sortType)!;
        var current = root;
        var segments = path.Split('.');
        for (var i = 0; i < segments.Length; i++)
        {
            var property = GraphQLMembers.Find(current.GetType(), segments[i])
                           ?? throw new InvalidOperationException($"'{path}' cannot be sorted: '{segments[i]}' is not on {current.GetType().Name}.");
            if (i == segments.Length - 1)
            {
                var enumType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                if (!enumType.IsEnum)
                {
                    throw new InvalidOperationException($"'{path}' cannot be sorted: it does not end in a sort direction.");
                }

                property.SetValue(current, SortDirection(enumType, descending));
            }
            else
            {
                var next = property.GetValue(current) ?? Activator.CreateInstance(property.PropertyType)!;
                property.SetValue(current, next);
                current = next;
            }
        }

        return root;
    }

    /// <summary>The member of the generated sort enum whose <c>[EnumMember]</c> value is ASC or DESC.</summary>
    protected virtual object SortDirection(Type enumType, bool descending)
    {
        var wanted = descending ? Descending : Ascending;
        foreach (var field in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var value = field.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? field.Name;
            if (string.Equals(value, wanted, StringComparison.OrdinalIgnoreCase))
            {
                return field.GetValue(null)!;
            }
        }

        throw new InvalidOperationException($"{enumType.Name} has no member for {wanted}.");
    }

    internal Type? FilterLeaf(Type filterType, string path)
    {
        var type = filterType;
        foreach (var segment in path.Split('.'))
        {
            var property = GraphQLMembers.Find(type, segment);
            if (property == null)
            {
                return null;
            }

            type = GraphQLMembers.ElementType(property.PropertyType);
        }

        return GraphQLMembers.IsLeafInput(type, And, Or) ? type : null;
    }

    internal static PropertyInfo? SortLeaf(Type sortType, string path)
    {
        var type = sortType;
        PropertyInfo? property = null;
        foreach (var segment in path.Split('.'))
        {
            property = GraphQLMembers.Find(type, segment);
            if (property == null)
            {
                return null;
            }

            type = GraphQLMembers.ElementType(property.PropertyType);
        }

        return property != null && type.IsEnum ? property : null;
    }
}

/// <summary>
/// Builds generated Linq2GraphQL filter inputs by path through their <c>[GraphQLMember]</c> properties:
/// <c>customer.name</c> + <c>contains "odi"</c> becomes <c>{ customer: { name: { contains: "odi" } } }</c>.
/// Conditions added with <see cref="Add"/> are combined with <c>and</c>.
/// </summary>
public sealed class InputBuilder
{
    private readonly HotChocolateConvention convention;
    private readonly List<object> conditions = [];

    public InputBuilder(Type filterType, HotChocolateConvention convention)
    {
        FilterType = filterType;
        this.convention = convention;
    }

    public Type FilterType { get; }

    public bool MatchesNothing { get; private set; }

    public void MarkMatchesNothing() => MatchesNothing = true;

    public FieldCapabilities Describe(string path) => convention.Describe(FilterType, typeof(object), path);

    public void Add(object condition) => conditions.Add(condition);

    /// <summary>A new filter input with the operations set on the leaf at <paramref name="path"/>.</summary>
    public object Condition(string path, params (string Operator, object? Value)[] operations)
    {
        var root = Activator.CreateInstance(FilterType)!;
        var current = root;
        var segments = path.Split('.');
        foreach (var segment in segments)
        {
            var property = GraphQLMembers.Find(current.GetType(), segment)
                           ?? throw new InvalidOperationException($"'{path}' cannot be filtered: '{segment}' is not on {current.GetType().Name}.");
            var next = property.GetValue(current) ?? Activator.CreateInstance(property.PropertyType)!;
            property.SetValue(current, next);
            current = next;
        }

        foreach (var (op, value) in operations)
        {
            var property = GraphQLMembers.Find(current.GetType(), op)
                           ?? throw new InvalidOperationException($"'{path}' does not support the '{op}' operator.");
            property.SetValue(current, ConvertTo(value, property.PropertyType));
        }

        return root;
    }

    public object AnyOf(IEnumerable<object> alternatives) => Combine(convention.Or, alternatives.ToList());

    public object AllOf(IEnumerable<object> parts) => Combine(convention.And, parts.ToList());

    /// <summary>The combined filter: null without conditions, the condition itself, or an <c>and</c> of them.</summary>
    public object? Build() => conditions.Count switch
    {
        0 => null,
        1 => conditions[0],
        _ => Combine(convention.And, conditions)
    };

    private object Combine(string member, List<object> parts)
    {
        if (parts.Count == 1)
        {
            return parts[0];
        }

        var root = Activator.CreateInstance(FilterType)!;
        var property = GraphQLMembers.Find(FilterType, member)
                       ?? throw new InvalidOperationException($"{FilterType.Name} has no '{member}' list.");
        var list = (IList)Activator.CreateInstance(property.PropertyType)!;
        foreach (var part in parts)
        {
            list.Add(part);
        }

        property.SetValue(root, list);
        return root;
    }

    // Values are converted to the operator property's exact type: nullable, enum, Guid, DateTimeOffset, List<T> for in.
    private static object? ConvertTo(object? value, Type type)
    {
        if (value == null)
        {
            return null;
        }

        var target = Nullable.GetUnderlyingType(type) ?? type;
        if (target != typeof(string) && target.IsGenericType && typeof(IList).IsAssignableFrom(target))
        {
            var elementType = target.GetGenericArguments()[0];
            var list = (IList)Activator.CreateInstance(target)!;
            foreach (var item in DataFilters.AsList(value))
            {
                list.Add(DataFilters.ConvertValue(item, elementType));
            }

            return list;
        }

        return DataFilters.ConvertValue(value, target);
    }
}
