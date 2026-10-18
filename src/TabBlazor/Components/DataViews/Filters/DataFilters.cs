#nullable enable
using System.Collections;
using System.Globalization;

namespace TabBlazor;

/// <summary>How column filters are shown.</summary>
public enum DataFilterMode
{
    /// <summary>A filter icon in each column header opens that column's filter (default).</summary>
    Menu,
    /// <summary>A row of filter controls under the column headers.</summary>
    Row
}

/// <summary>The context of a column's <c>FilterTemplate</c>.</summary>
public sealed class DataFilterContext<TItem>
{
    private readonly DataView<TItem> view;

    internal DataFilterContext(DataView<TItem> view, IDataViewColumn<TItem> column)
    {
        this.view = view;
        Column = column;
    }

    public IDataViewColumn<TItem> Column { get; }

    /// <summary>The column's active filter, or null.</summary>
    public FilterSpec? Filter => view.GetFilter(Column.Id);

    /// <summary>Sets (or with null, clears) the column's filter and reloads.</summary>
    public Task SetAsync(FilterSpec? filter) => view.SetColumnFilterAsync(Column.Id, filter);
}

/// <summary>Rules for column filters: which operators a column allows, and how filters read and are written to URLs.</summary>
public static class DataFilters
{
    private static readonly FilterOperator[] TextOperators =
    [
        FilterOperator.Contains, FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.StartsWith,
        FilterOperator.EndsWith, FilterOperator.In, FilterOperator.IsNull, FilterOperator.IsNotNull
    ];

    private static readonly FilterOperator[] RangeOperators =
    [
        FilterOperator.Between, FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.GreaterThan,
        FilterOperator.GreaterThanOrEqual, FilterOperator.LessThan, FilterOperator.LessThanOrEqual, FilterOperator.In,
        FilterOperator.IsNull, FilterOperator.IsNotNull
    ];

    private static readonly FilterOperator[] PartsOperators =
        [FilterOperator.Contains, FilterOperator.StartsWith, FilterOperator.IsNull, FilterOperator.IsNotNull];

    /// <summary>
    /// The operators a column allows: one default operator per kind of value (plus "empty" and "not empty" for
    /// nullable values), or the configured ones that make sense for the kind.
    /// </summary>
    public static IReadOnlyList<FilterOperator> AllowedOperators(DataColumnInfo info, IReadOnlyList<FilterOperator>? configured)
    {
        var valid = ValidOperators(info);
        if (configured is { Count: > 0 })
        {
            return configured.Where(valid.Contains).Distinct().ToList();
        }

        var defaults = new List<FilterOperator>();
        if (info.FilterMode == FilterPathMode.Parts)
        {
            defaults.Add(FilterOperator.Contains);
        }
        else
        {
            switch (info.Kind)
            {
                case DataValueKind.Text:
                    defaults.Add(FilterOperator.Contains);
                    break;
                case DataValueKind.Number:
                case DataValueKind.Date:
                    defaults.Add(FilterOperator.Between);
                    break;
                case DataValueKind.Boolean:
                case DataValueKind.Guid:
                    defaults.Add(FilterOperator.Equals);
                    break;
                case DataValueKind.Enum:
                    defaults.Add(FilterOperator.In);
                    break;
            }
        }

        if (defaults.Count > 0 && Nullable.GetUnderlyingType(info.FilterValueType ?? typeof(object)) != null)
        {
            defaults.Add(FilterOperator.IsNull);
            defaults.Add(FilterOperator.IsNotNull);
        }

        return defaults;
    }

    private static IReadOnlyList<FilterOperator> ValidOperators(DataColumnInfo info)
    {
        if (info.FilterMode == FilterPathMode.Parts)
        {
            return PartsOperators;
        }

        return info.Kind switch
        {
            DataValueKind.Text => TextOperators,
            DataValueKind.Number or DataValueKind.Date => RangeOperators,
            DataValueKind.Boolean => [FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.IsNull, FilterOperator.IsNotNull],
            DataValueKind.Enum or DataValueKind.Guid =>
                [FilterOperator.In, FilterOperator.Equals, FilterOperator.NotEquals, FilterOperator.IsNull, FilterOperator.IsNotNull],
            _ => []
        };
    }

    /// <summary>
    /// Converts a filter value (typed, a string from a URL, or a <c>JsonElement</c>) to <paramref name="type"/>, using
    /// the invariant culture and ISO 8601 dates. For sources that build their own queries.
    /// </summary>
    public static object? ConvertValue(object? value, Type type) => InMemoryFilters.ConvertValue(value, type);

    /// <summary>True when the operator needs no value.</summary>
    public static bool HasNoValue(FilterOperator op) => op is FilterOperator.IsNull or FilterOperator.IsNotNull;

    /// <summary>Converts a filter's values to the column's type; null when they cannot be converted or are missing.</summary>
    public static FilterSpec? Normalize(FilterSpec filter, DataColumnInfo info)
    {
        if (HasNoValue(filter.Operator))
        {
            return filter with { Value = null, Value2 = null };
        }

        var type = info.FilterMode == FilterPathMode.Parts ? typeof(string) : info.FilterValueType ?? info.ValueType;
        try
        {
            switch (filter.Operator)
            {
                case FilterOperator.In:
                    var values = AsList(filter.Value).Select(v => InMemoryFilters.ConvertValue(v, type)).Where(v => v != null).ToList();
                    return values.Count == 0 ? null : filter with { Value = values, Value2 = null };
                case FilterOperator.Between:
                    var from = InMemoryFilters.ConvertValue(filter.Value, type);
                    var to = InMemoryFilters.ConvertValue(filter.Value2, type);
                    return from == null && to == null ? null : filter with { Value = from, Value2 = to };
                default:
                    var value = InMemoryFilters.ConvertValue(filter.Value, type);
                    return value is null or "" ? null : filter with { Value = value, Value2 = null };
            }
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            return null;
        }
    }

    internal static IEnumerable<object?> AsList(object? value) => value switch
    {
        null => [],
        string s => s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        IEnumerable e => e.Cast<object?>(),
        _ => [value]
    };

    /// <summary>The token for an operator in URLs: eq, neq, contains, startsWith, endsWith, gt, gte, lt, lte, between, in, isNull, isNotNull.</summary>
    public static string Token(FilterOperator op) => op switch
    {
        FilterOperator.Equals => "eq",
        FilterOperator.NotEquals => "neq",
        FilterOperator.Contains => "contains",
        FilterOperator.StartsWith => "startsWith",
        FilterOperator.EndsWith => "endsWith",
        FilterOperator.GreaterThan => "gt",
        FilterOperator.GreaterThanOrEqual => "gte",
        FilterOperator.LessThan => "lt",
        FilterOperator.LessThanOrEqual => "lte",
        FilterOperator.Between => "between",
        FilterOperator.In => "in",
        FilterOperator.IsNull => "isNull",
        _ => "isNotNull"
    };

    public static FilterOperator? ParseToken(string token) =>
        Enum.GetValues<FilterOperator>().Cast<FilterOperator?>()
            .FirstOrDefault(op => string.Equals(Token(op!.Value), token, StringComparison.OrdinalIgnoreCase));

    /// <summary>Formats a filter value for URLs: invariant culture, ISO 8601 dates, enum names.</summary>
    public static string FormatValue(object? value) => value switch
    {
        null => string.Empty,
        DateTime d => d.TimeOfDay == TimeSpan.Zero ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : d.ToString("o", CultureInfo.InvariantCulture),
        DateTimeOffset d => d.ToString("o", CultureInfo.InvariantCulture),
        DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        bool b => b ? "true" : "false",
        Enum e => e.ToString(),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty
    };

    /// <summary>A short text for a filter, for chips: "contains odi", "2026-01-01 – 2026-03-31", "Open, Closed".</summary>
    public static string Describe(FilterSpec filter, DataViewText texts)
    {
        string Show(object? v) => v switch
        {
            null => "…",
            Enum e => EnumDisplay.GetName(e),
            DateTime d => d.ToString("d", CultureInfo.CurrentCulture),
            DateTimeOffset d => d.ToString("d", CultureInfo.CurrentCulture),
            DateOnly d => d.ToString("d", CultureInfo.CurrentCulture),
            bool b => b ? texts.Yes : texts.No,
            IFormattable f => f.ToString(null, CultureInfo.CurrentCulture),
            _ => v.ToString() ?? string.Empty
        };

        return filter.Operator switch
        {
            FilterOperator.IsNull => texts.IsEmpty,
            FilterOperator.IsNotNull => texts.IsNotEmpty,
            FilterOperator.In => string.Join(", ", AsList(filter.Value).Select(Show)),
            FilterOperator.Between when filter.Value2 == null => $"≥ {Show(filter.Value)}",
            FilterOperator.Between when filter.Value == null => $"≤ {Show(filter.Value2)}",
            FilterOperator.Between => $"{Show(filter.Value)} – {Show(filter.Value2)}",
            FilterOperator.Equals => Show(filter.Value),
            _ => $"{OperatorText(filter.Operator, texts)} {Show(filter.Value)}"
        };
    }

    public static string OperatorText(FilterOperator op, DataViewText texts) => op switch
    {
        FilterOperator.Equals => texts.OpEquals,
        FilterOperator.NotEquals => texts.OpNotEquals,
        FilterOperator.Contains => texts.OpContains,
        FilterOperator.StartsWith => texts.OpStartsWith,
        FilterOperator.EndsWith => texts.OpEndsWith,
        FilterOperator.GreaterThan => ">",
        FilterOperator.GreaterThanOrEqual => "≥",
        FilterOperator.LessThan => "<",
        FilterOperator.LessThanOrEqual => "≤",
        FilterOperator.Between => texts.OpBetween,
        FilterOperator.In => texts.OpIn,
        FilterOperator.IsNull => texts.IsEmpty,
        _ => texts.IsNotEmpty
    };
}
