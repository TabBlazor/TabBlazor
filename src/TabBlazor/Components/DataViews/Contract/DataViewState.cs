#nullable enable
namespace TabBlazor;

/// <summary>
/// The serializable state of a <c>DataView</c>: paging, sort, search and filters. Columns are referred to by
/// their <see cref="DataColumnInfo.Id"/>, with the same operators as <see cref="DataRequest"/>.
/// Two states are equal when all their values are equal.
/// </summary>
public sealed record DataViewState
{
    /// <summary>The 1-based page number.</summary>
    public int Page { get; init; } = 1;

    /// <summary>The page size.</summary>
    public int PageSize { get; init; }

    /// <summary>The sorts, in priority order (one in version 1).</summary>
    public IReadOnlyList<SortSpec> Sorts { get; init; } = [];

    /// <summary>The global search text, or null.</summary>
    public string? Search { get; init; }

    /// <summary>The column filters.</summary>
    public IReadOnlyList<FilterSpec> Filters { get; init; } = [];

    public bool Equals(DataViewState? other) =>
        other is not null &&
        Page == other.Page &&
        PageSize == other.PageSize &&
        string.Equals(Search ?? string.Empty, other.Search ?? string.Empty, StringComparison.Ordinal) &&
        Sorts.SequenceEqual(other.Sorts) &&
        Filters.SequenceEqual(other.Filters, FilterSpecComparer.Instance);

    public override int GetHashCode() => HashCode.Combine(Page, PageSize, Search ?? string.Empty, Sorts.Count, Filters.Count);

    private sealed class FilterSpecComparer : IEqualityComparer<FilterSpec>
    {
        public static FilterSpecComparer Instance { get; } = new();

        public bool Equals(FilterSpec? x, FilterSpec? y) =>
            x is not null && y is not null &&
            x.Column == y.Column && x.Operator == y.Operator &&
            ValueEquals(x.Value, y.Value) && ValueEquals(x.Value2, y.Value2);

        public int GetHashCode(FilterSpec obj) => HashCode.Combine(obj.Column, obj.Operator);

        private static bool ValueEquals(object? a, object? b)
        {
            if (Equals(a, b))
            {
                return true;
            }

            if (a is System.Collections.IEnumerable ea and not string && b is System.Collections.IEnumerable eb and not string)
            {
                return ea.Cast<object?>().SequenceEqual(eb.Cast<object?>());
            }

            return false;
        }
    }
}
