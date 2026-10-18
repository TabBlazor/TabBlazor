#nullable enable
using System.Collections;
using System.Globalization;
using System.Text.Json;

namespace TabBlazor;

/// <summary>Evaluates <see cref="FilterSpec"/>s in memory, with the expressions carried in <see cref="DataColumnInfo"/>.</summary>
internal static class InMemoryFilters
{
    public static IEnumerable<TItem> Apply<TItem>(IEnumerable<TItem> query, DataRequest request,
        Func<LambdaExpression, Func<TItem, object?>> getGetter)
    {
        foreach (var filter in request.Filters)
        {
            var column = request.GetColumn(filter.Column);
            if (column.FilterExpressions.Count == 0)
            {
                throw new InvalidOperationException($"Column '{column.Id}' has no value or FilterBy to filter on.");
            }

            var predicate = column.FilterExpressions.Count == 1 && column.FilterMode == FilterPathMode.Exact
                ? CreateExact(column.FilterExpressions[0], filter, getGetter)
                : CreateParts(column, filter, getGetter);
            query = query.Where(predicate);
        }

        return query;
    }

    private static Func<TItem, bool> CreateExact<TItem>(LambdaExpression part, FilterSpec filter,
        Func<LambdaExpression, Func<TItem, object?>> getGetter)
    {
        var getter = getGetter(part);
        var type = part.ReturnType;
        var match = CreateValuePredicate(type, filter);
        return item => match(getter(item));
    }

    // Several parts: each word of the filter value must match one of the parts (Contains or StartsWith).
    private static Func<TItem, bool> CreateParts<TItem>(DataColumnInfo column, FilterSpec filter,
        Func<LambdaExpression, Func<TItem, object?>> getGetter)
    {
        if (filter.Operator is FilterOperator.IsNull or FilterOperator.IsNotNull)
        {
            var getters = column.FilterExpressions.Select(getGetter).ToList();
            var isNull = filter.Operator == FilterOperator.IsNull;
            return item => getters.All(g => IsEmpty(g(item))) == isNull;
        }

        if (filter.Operator is not (FilterOperator.Contains or FilterOperator.StartsWith))
        {
            throw new InvalidOperationException(
                $"Column '{column.Id}' filters on several parts, which only support Contains and StartsWith (got {filter.Operator}).");
        }

        var words = SearchSpec.ParseTerms(ToText(filter.Value) ?? string.Empty, int.MaxValue);
        var parts = column.FilterExpressions.Select(getGetter).ToList();
        var startsWith = filter.Operator == FilterOperator.StartsWith;
        return item => words.All(word => parts.Any(part =>
        {
            var text = part(item)?.ToString();
            return text != null && (startsWith
                ? text.StartsWith(word, StringComparison.CurrentCultureIgnoreCase)
                : CultureInfo.CurrentCulture.CompareInfo.IndexOf(text, word, CompareOptions.IgnoreCase) >= 0);
        }));
    }

    internal static Func<object?, bool> CreateValuePredicate(Type type, FilterSpec filter)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        var isText = underlying == typeof(string);
        switch (filter.Operator)
        {
            case FilterOperator.IsNull:
                return IsEmpty;
            case FilterOperator.IsNotNull:
                return v => !IsEmpty(v);
            case FilterOperator.Contains:
            case FilterOperator.StartsWith:
            case FilterOperator.EndsWith:
            {
                var term = ToText(filter.Value) ?? string.Empty;
                var op = filter.Operator;
                return v =>
                {
                    var text = v is Enum e ? EnumDisplay.GetName(e) : v?.ToString();
                    if (text == null)
                    {
                        return false;
                    }

                    return op switch
                    {
                        FilterOperator.StartsWith => text.StartsWith(term, StringComparison.CurrentCultureIgnoreCase),
                        FilterOperator.EndsWith => text.EndsWith(term, StringComparison.CurrentCultureIgnoreCase),
                        _ => CultureInfo.CurrentCulture.CompareInfo.IndexOf(text, term, CompareOptions.IgnoreCase) >= 0
                    };
                };
            }
            case FilterOperator.Equals:
            case FilterOperator.NotEquals:
            {
                var target = ConvertValue(filter.Value, underlying);
                var negate = filter.Operator == FilterOperator.NotEquals;
                return v => AreEqual(v, target, isText) != negate;
            }
            case FilterOperator.In:
            {
                var targets = ToList(filter.Value).Select(x => ConvertValue(x, underlying)).ToList();
                return v => targets.Any(t => AreEqual(v, t, isText));
            }
            case FilterOperator.GreaterThan:
            case FilterOperator.GreaterThanOrEqual:
            case FilterOperator.LessThan:
            case FilterOperator.LessThanOrEqual:
            {
                var target = ConvertValue(filter.Value, underlying);
                var op = filter.Operator;
                var comparer = new DataValueComparer(false);
                return v =>
                {
                    if (v == null || target == null)
                    {
                        return false;
                    }

                    var c = comparer.Compare(v, target);
                    return op switch
                    {
                        FilterOperator.GreaterThan => c > 0,
                        FilterOperator.GreaterThanOrEqual => c >= 0,
                        FilterOperator.LessThan => c < 0,
                        _ => c <= 0
                    };
                };
            }
            case FilterOperator.Between:
            {
                var from = ConvertValue(filter.Value, underlying);
                var to = ConvertValue(filter.Value2, underlying);
                var comparer = new DataValueComparer(false);
                return v => v != null &&
                            (from == null || comparer.Compare(v, from) >= 0) &&
                            (to == null || comparer.Compare(v, to) <= 0);
            }
            default:
                throw new NotSupportedException($"Filter operator {filter.Operator} is not supported.");
        }
    }

    private static bool IsEmpty(object? value) => value == null || value is string { Length: 0 };

    private static bool AreEqual(object? value, object? target, bool isText)
    {
        if (value == null || target == null)
        {
            return value == null && target == null;
        }

        if (isText)
        {
            return string.Equals(value.ToString(), target.ToString(), StringComparison.CurrentCultureIgnoreCase);
        }

        return value.Equals(target);
    }

    private static string? ToText(object? value) => value switch
    {
        null => null,
        JsonElement { ValueKind: JsonValueKind.String } e => e.GetString(),
        JsonElement e => e.GetRawText(),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()
    };

    private static IEnumerable<object?> ToList(object? value) => value switch
    {
        null => [],
        string s => [s],
        JsonElement { ValueKind: JsonValueKind.Array } e => e.EnumerateArray().Select(x => (object?)x),
        IEnumerable enumerable => enumerable.Cast<object?>(),
        _ => [value]
    };

    /// <summary>
    /// Converts a filter value (a typed value, a string from a URL, or a <see cref="JsonElement"/> from a
    /// deserialized request) to <paramref name="type"/>. Strings are parsed with the invariant culture, dates as ISO 8601.
    /// </summary>
    public static object? ConvertValue(object? value, Type type)
    {
        var target = Nullable.GetUnderlyingType(type) ?? type;
        if (value is JsonElement element)
        {
            value = element.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                JsonValueKind.String => element.GetString(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number => element.GetDecimal(),
                _ => element.GetRawText()
            };
        }

        if (value == null || target.IsInstanceOfType(value))
        {
            return value;
        }

        var invariant = CultureInfo.InvariantCulture;
        if (value is string s)
        {
            if (target == typeof(string)) return s;
            if (target.IsEnum) return Enum.Parse(target, s, true);
            if (target == typeof(Guid)) return Guid.Parse(s);
            if (target == typeof(DateTime)) return DateTime.Parse(s, invariant, DateTimeStyles.RoundtripKind);
            if (target == typeof(DateTimeOffset)) return DateTimeOffset.Parse(s, invariant, DateTimeStyles.RoundtripKind);
            if (target == typeof(DateOnly)) return DateOnly.FromDateTime(DateTime.Parse(s, invariant));
            if (target == typeof(TimeOnly)) return TimeOnly.Parse(s, invariant);
            if (target == typeof(bool)) return bool.Parse(s);
            return Convert.ChangeType(s, target, invariant);
        }

        return value switch
        {
            DateOnly d when target == typeof(DateTime) => d.ToDateTime(TimeOnly.MinValue),
            DateOnly d when target == typeof(DateTimeOffset) => new DateTimeOffset(d.ToDateTime(TimeOnly.MinValue)),
            DateTime dt when target == typeof(DateOnly) => DateOnly.FromDateTime(dt),
            DateTime dt when target == typeof(DateTimeOffset) => new DateTimeOffset(dt),
            DateTimeOffset dto when target == typeof(DateTime) => dto.DateTime,
            DateTimeOffset dto when target == typeof(DateOnly) => DateOnly.FromDateTime(dto.DateTime),
            _ when target.IsEnum => Enum.ToObject(target, value),
            _ when target == typeof(string) => ToText(value),
            _ => Convert.ChangeType(value, target, invariant)
        };
    }
}
