#nullable enable
using System.Globalization;
using System.Runtime.CompilerServices;

namespace TabBlazor;

/// <summary>Options for <see cref="InMemorySource{TItem}"/>.</summary>
public sealed record InMemorySourceOptions
{
    /// <summary>The paging mode to expose. Cursor mode is mainly useful for tests and demos. Defaults to Offset.</summary>
    public PagingMode PagingMode { get; init; } = PagingMode.Offset;

    /// <summary>When false, <see cref="DataPage{TItem}.TotalCount"/> is left null. Defaults to true.</summary>
    public bool IncludeTotalCount { get; init; } = true;

    /// <summary>Sorts text with embedded numbers naturally ("item 2" before "item 10"). Defaults to false.</summary>
    public bool NaturalSort { get; init; }
}

/// <summary>
/// A <see cref="IDataViewSource{TItem}"/> over an in-memory list, used by <c>DataView.Items</c>. It sorts,
/// searches and filters with the expressions carried in <see cref="DataColumnInfo"/>, so calculated columns
/// work exactly on their displayed value. The list is read on every request, so changes are picked up on reload.
/// </summary>
public sealed class InMemorySource<TItem> : IDataViewSource<TItem>
{
    private readonly ConditionalWeakTable<LambdaExpression, Func<TItem, object?>> getters = new();
    private readonly ConditionalWeakTable<LambdaExpression, Func<TItem, string, bool>> searchPredicates = new();
    private readonly InMemorySourceOptions options;

    public InMemorySource(IEnumerable<TItem> items, InMemorySourceOptions? options = null)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
        this.options = options ?? new InMemorySourceOptions();
    }

    /// <summary>The list the source reads from.</summary>
    public IEnumerable<TItem> Items { get; }

    public PagingMode PagingMode => options.PagingMode;

    public Task<DataPage<TItem>> GetPageAsync(DataRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GetPage(request));
    }

    /// <summary>Runs the request synchronously.</summary>
    public DataPage<TItem> GetPage(DataRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        IEnumerable<TItem> query = Items.ToList();

        if (request.Filters.Count > 0)
        {
            query = InMemoryFilters.Apply(query, request, GetGetter);
        }

        if (request.Search is { Terms.Count: > 0 } search)
        {
            query = ApplySearch(query, request, search);
        }

        query = ApplySorts(query, request);

        var all = query as IList<TItem> ?? query.ToList();
        int skip;
        int take = Math.Max(0, request.Page.Take);
        if (request.Page.Mode == PagingMode.Cursor)
        {
            skip = string.IsNullOrEmpty(request.Page.After)
                ? 0
                : int.Parse(request.Page.After, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }
        else
        {
            skip = Math.Max(0, request.Page.Skip);
        }

        var pageItems = all.Skip(skip).Take(take).ToList();
        string? nextCursor = null;
        if (request.Page.Mode == PagingMode.Cursor && skip + take < all.Count)
        {
            nextCursor = (skip + take).ToString(CultureInfo.InvariantCulture);
        }

        return new DataPage<TItem>(pageItems, options.IncludeTotalCount ? all.Count : null, nextCursor);
    }

    private IEnumerable<TItem> ApplySearch(IEnumerable<TItem> query, DataRequest request, SearchSpec search)
    {
        var columns = search.Columns.Select(request.GetColumn).ToList();

        // One list of matchers per term: a row matches when every term matches at least one column.
        var termMatchers = search.Terms
            .Select(term => columns.SelectMany(c => CreateMatchers(c, term)).ToList())
            .ToList();

        return query.Where(item => termMatchers.All(matchers => matchers.Any(m => m(item))));
    }

    private IEnumerable<Func<TItem, bool>> CreateMatchers(DataColumnInfo column, string term)
    {
        if (column.SearchExpression != null)
        {
            var predicate = searchPredicates.GetValue(column.SearchExpression,
                e => (Func<TItem, string, bool>)e.Compile());
            yield return item => SafeInvoke(() => predicate(item, term));
            yield break;
        }

        foreach (var part in column.FilterExpressions)
        {
            var valueMatcher = CreateValueMatcher(part.ReturnType, term);
            if (valueMatcher == null)
            {
                continue;
            }

            var getter = GetGetter(part);
            yield return item => valueMatcher(getter(item));
        }
    }

    // Matching by column type: text contains, numbers and Guids equal, enums by display name; dates and bools are not searched.
    internal static Func<object?, bool>? CreateValueMatcher(Type type, string term)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        switch (DataColumnInfo.KindOf(underlying))
        {
            case DataValueKind.Text:
                return v => v != null && Contains(v.ToString(), term);
            case DataValueKind.Number:
                if (!TryParseNumber(term, out var number))
                {
                    return null;
                }

                return v =>
                {
                    if (v == null)
                    {
                        return false;
                    }

                    try
                    {
                        return Convert.ToDecimal(v, CultureInfo.InvariantCulture) == number;
                    }
                    catch (OverflowException)
                    {
                        return false;
                    }
                };
            case DataValueKind.Enum:
                return v => v != null && Contains(EnumDisplay.GetName((Enum)v), term);
            case DataValueKind.Guid:
                return Guid.TryParse(term, out var guid) ? v => v is Guid g && g == guid : null;
            case DataValueKind.Other when underlying == typeof(object):
                return v => v != null && Contains(v.ToString(), term);
            default:
                return null;
        }
    }

    private static bool Contains(string? value, string term) =>
        value != null && CultureInfo.CurrentCulture.CompareInfo.IndexOf(value, term, CompareOptions.IgnoreCase) >= 0;

    internal static bool TryParseNumber(string term, out decimal number) =>
        decimal.TryParse(term, NumberStyles.Number, CultureInfo.CurrentCulture, out number) ||
        decimal.TryParse(term, NumberStyles.Number, CultureInfo.InvariantCulture, out number);

    private IEnumerable<TItem> ApplySorts(IEnumerable<TItem> query, DataRequest request)
    {
        IOrderedEnumerable<TItem>? ordered = null;
        var comparer = new DataValueComparer(options.NaturalSort);
        foreach (var sort in request.Sorts)
        {
            var column = request.GetColumn(sort.Column);
            if (column.SortExpressions.Count == 0)
            {
                throw new InvalidOperationException($"Column '{column.Id}' has no value or SortBy to sort by.");
            }

            foreach (var expression in column.SortExpressions)
            {
                var getter = GetGetter(expression);
                ordered = ordered == null
                    ? sort.Descending ? query.OrderByDescending(getter, comparer) : query.OrderBy(getter, comparer)
                    : sort.Descending ? ordered.ThenByDescending(getter, comparer) : ordered.ThenBy(getter, comparer);
            }
        }

        return ordered ?? query;
    }

    private Func<TItem, object?> GetGetter(LambdaExpression expression) =>
        getters.GetValue(expression, CompileGetter);

    private static Func<TItem, object?> CompileGetter(LambdaExpression expression)
    {
        var body = Expression.Convert(expression.Body, typeof(object));
        var compiled = Expression.Lambda<Func<TItem, object?>>(body, expression.Parameters).Compile();
        return item => SafeInvoke(() => compiled(item));
    }

    // A null somewhere in a member chain (o.Customer.Name with no customer) yields null instead of throwing.
    private static T? SafeInvoke<T>(Func<T> func)
    {
        try
        {
            return func();
        }
        catch (NullReferenceException)
        {
            return default;
        }
    }
}

/// <summary>Compares sort keys: nulls first, text by culture (optionally natural), other values by <see cref="IComparable"/>.</summary>
internal sealed class DataValueComparer(bool natural) : IComparer<object?>
{
    public int Compare(object? x, object? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x == null)
        {
            return -1;
        }

        if (y == null)
        {
            return 1;
        }

        if (x is string sx && y is string sy)
        {
            return natural ? NaturalStringComparer.Instance.Compare(sx, sy) : string.Compare(sx, sy, StringComparison.CurrentCulture);
        }

        if (x is IComparable comparable && x.GetType() == y.GetType())
        {
            return comparable.CompareTo(y);
        }

        return string.Compare(x.ToString(), y.ToString(), StringComparison.CurrentCulture);
    }
}

/// <summary>Compares text with embedded numbers by their numeric value ("item 2" before "item 10").</summary>
internal sealed class NaturalStringComparer : IComparer<string?>
{
    public static NaturalStringComparer Instance { get; } = new();

    public int Compare(string? x, string? y)
    {
        if (x == null || y == null)
        {
            return x == null ? (y == null ? 0 : -1) : 1;
        }

        int i = 0, j = 0;
        while (i < x.Length && j < y.Length)
        {
            if (char.IsDigit(x[i]) && char.IsDigit(y[j]))
            {
                var si = i;
                var sj = j;
                while (i < x.Length && char.IsDigit(x[i])) i++;
                while (j < y.Length && char.IsDigit(y[j])) j++;
                var nx = x.AsSpan(si, i - si).TrimStart('0');
                var ny = y.AsSpan(sj, j - sj).TrimStart('0');
                if (nx.Length != ny.Length)
                {
                    return nx.Length.CompareTo(ny.Length);
                }

                var c = nx.CompareTo(ny, StringComparison.Ordinal);
                if (c != 0)
                {
                    return c;
                }
            }
            else
            {
                var si = i;
                var sj = j;
                while (i < x.Length && !char.IsDigit(x[i])) i++;
                while (j < y.Length && !char.IsDigit(y[j])) j++;
                var c = string.Compare(x.Substring(si, i - si), y.Substring(sj, j - sj), StringComparison.CurrentCulture);
                if (c != 0)
                {
                    return c;
                }
            }
        }

        return (x.Length - i).CompareTo(y.Length - j);
    }
}
