#nullable enable
using System.Text.Json.Serialization;

namespace TabBlazor;

/// <summary>
/// Everything a source needs to load one page: paging, sorts, global search, column filters and the columns
/// they refer to. Columns are referred to by their stable <see cref="DataColumnInfo.Id"/>. The request
/// serializes cleanly to JSON (expressions and CLR types are skipped), so it can be posted to an API.
/// </summary>
/// <remarks>
/// A request that comes from a browser must not be trusted: a server must check every column, path and
/// operator against an allow-list before using it.
/// </remarks>
public sealed record DataRequest(
    PageRequest Page,
    IReadOnlyList<SortSpec> Sorts,
    SearchSpec? Search,
    IReadOnlyList<FilterSpec> Filters,
    IReadOnlyList<DataColumnInfo> Columns)
{
    /// <summary>Finds a column by its <see cref="DataColumnInfo.Id"/>, or null.</summary>
    public DataColumnInfo? FindColumn(string id) =>
        Columns.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.Ordinal));

    /// <summary>Finds a column by its id, or throws a clear error when the request refers to an unknown column.</summary>
    public DataColumnInfo GetColumn(string id) =>
        FindColumn(id) ?? throw new InvalidOperationException(
            $"The request refers to column '{id}', which is not in DataRequest.Columns.");
}

/// <summary>
/// The page to load. For <see cref="PagingMode.Offset"/> use <see cref="Skip"/> and <see cref="Take"/>; for
/// <see cref="PagingMode.Cursor"/> use <see cref="After"/> and <see cref="First"/> (the same value as Take).
/// </summary>
public sealed record PageRequest(PagingMode Mode, int Skip, int Take, string? After = null)
{
    /// <summary>The page size for cursor paging (same as <see cref="Take"/>).</summary>
    [JsonIgnore] public int First => Take;

    public static PageRequest Offset(int skip, int take) => new(PagingMode.Offset, skip, take);

    public static PageRequest Cursor(string? after, int first) => new(PagingMode.Cursor, 0, first, after);
}

/// <summary>Sort by one column, referred to by <see cref="DataColumnInfo.Id"/>.</summary>
public sealed record SortSpec(string Column, bool Descending = false);

/// <summary>How the global search text is matched.</summary>
public enum SearchMode
{
    /// <summary>Every word (or quoted phrase) must appear in at least one searchable column.</summary>
    Words,
    /// <summary>The whole text must appear in one searchable column (Table's behaviour).</summary>
    Phrase
}

/// <summary>
/// The global search. <see cref="Terms"/> holds the parsed words and quoted phrases (an <c>And</c> of
/// <c>Or</c>s across <see cref="Columns"/>); <see cref="Text"/> is the raw text for servers with full-text search.
/// </summary>
public sealed record SearchSpec(string Text, IReadOnlyList<string> Terms, SearchMode Mode, IReadOnlyList<string> Columns)
{
    /// <summary>The default maximum number of search terms.</summary>
    public const int DefaultMaxTerms = 5;

    /// <summary>
    /// Parses search text into terms. In <see cref="SearchMode.Words"/> mode words are split on whitespace,
    /// double quotes keep a phrase together, duplicates are removed and at most <paramref name="maxTerms"/>
    /// terms are kept. Returns null when there is nothing to search for.
    /// </summary>
    public static SearchSpec? Parse(string? text, SearchMode mode, IReadOnlyList<string> columns,
        int maxTerms = DefaultMaxTerms)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var trimmed = text.Trim();
        var terms = mode == SearchMode.Phrase ? new List<string> { trimmed } : ParseTerms(trimmed, maxTerms);
        return terms.Count == 0 ? null : new SearchSpec(trimmed, terms, mode, columns);
    }

    /// <summary>Splits text into words and quoted phrases.</summary>
    public static List<string> ParseTerms(string text, int maxTerms = DefaultMaxTerms)
    {
        var terms = new List<string>();
        var i = 0;
        while (i < text.Length && terms.Count < Math.Max(1, maxTerms))
        {
            if (char.IsWhiteSpace(text[i]))
            {
                i++;
                continue;
            }

            string term;
            if (text[i] == '"')
            {
                var end = text.IndexOf('"', i + 1);
                if (end < 0)
                {
                    end = text.Length;
                }

                term = text.Substring(i + 1, end - i - 1).Trim();
                i = end + 1;
            }
            else
            {
                var start = i;
                while (i < text.Length && !char.IsWhiteSpace(text[i]) && text[i] != '"')
                {
                    i++;
                }

                term = text.Substring(start, i - start);
            }

            if (term.Length > 0 && !terms.Contains(term, StringComparer.CurrentCultureIgnoreCase))
            {
                terms.Add(term);
            }
        }

        return terms;
    }
}

/// <summary>A column filter, referred to by <see cref="DataColumnInfo.Id"/>.</summary>
/// <remarks>After JSON deserialization <see cref="Value"/> is a <c>JsonElement</c>; convert it by the column's <see cref="DataColumnInfo.Kind"/>.</remarks>
public sealed record FilterSpec(string Column, FilterOperator Operator, object? Value = null, object? Value2 = null);

/// <summary>Filter operators. The names follow HotChocolate's (<c>eq</c>, <c>contains</c>, <c>gte</c>, <c>in</c>, …).</summary>
public enum FilterOperator
{
    Equals,
    NotEquals,
    Contains,
    StartsWith,
    EndsWith,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Between,
    In,
    IsNull,
    IsNotNull
}

/// <summary>How a column's filter and search paths are used.</summary>
public enum FilterPathMode
{
    /// <summary>One path, filtered as if the column were bound to it (its type and all its operators).</summary>
    Exact,
    /// <summary>Several paths: each search word or filter value must match one of them (Contains and StartsWith only).</summary>
    Parts
}

/// <summary>The kind of value a column filters and searches on, for sources that cannot see CLR types.</summary>
public enum DataValueKind
{
    Text,
    Number,
    Boolean,
    Date,
    Enum,
    Guid,
    Other
}

/// <summary>
/// Describes a column to a source: its stable <see cref="Id"/> and the member paths to sort, filter and search on.
/// Remote sources use the paths; in-process sources can use the expressions, which are not serialized.
/// </summary>
/// <param name="Id">The column's stable name, for example <c>customer.name</c> or <c>fullName</c>.</param>
/// <param name="ValueType">The type of the displayed value. Not serialized.</param>
/// <param name="SortPaths">Member paths to sort by, in order (from <c>Value</c> or <c>SortBy</c>). Empty when the column cannot be sorted by path.</param>
/// <param name="FilterPaths">Member paths to filter and search on (from <c>Value</c> or <c>FilterBy</c>). Empty when the column cannot be filtered by path.</param>
/// <param name="FilterMode">Exact (one path) or Parts (several).</param>
/// <param name="ValueExpression">The column's value expression, for in-process sources only. Not serialized.</param>
public sealed record DataColumnInfo(
    string Id,
    [property: JsonIgnore] Type ValueType,
    IReadOnlyList<string> SortPaths,
    IReadOnlyList<string> FilterPaths,
    FilterPathMode FilterMode,
    [property: JsonIgnore] LambdaExpression? ValueExpression)
{
    /// <summary>The kind of value the column filters and searches on (from <c>FilterBy</c> when set, else from <c>Value</c>).</summary>
    public DataValueKind Kind { get; init; } = DataValueKind.Other;

    /// <summary>The CLR type the column filters and searches on. Not serialized.</summary>
    [JsonIgnore] public Type? FilterValueType { get; init; }

    /// <summary>Expressions to sort by in order, for in-process sources (<c>SortBy</c> parts, or <c>Value</c>). Not serialized.</summary>
    [JsonIgnore] public IReadOnlyList<LambdaExpression> SortExpressions { get; init; } = [];

    /// <summary>Expressions to filter and search on, for in-process sources (<c>FilterBy</c> parts, or <c>Value</c>). Not serialized.</summary>
    [JsonIgnore] public IReadOnlyList<LambdaExpression> FilterExpressions { get; init; } = [];

    /// <summary>A custom in-memory search predicate (<c>Func&lt;TItem, string, bool&gt;</c>), from the column's <c>SearchExpression</c>. Not serialized.</summary>
    [JsonIgnore] public LambdaExpression? SearchExpression { get; init; }

    /// <summary>Gets the <see cref="DataValueKind"/> for a CLR type.</summary>
    public static DataValueKind KindOf(Type? type)
    {
        if (type == null)
        {
            return DataValueKind.Other;
        }

        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type == typeof(string) || type == typeof(char))
        {
            return DataValueKind.Text;
        }

        if (type.IsEnum)
        {
            return DataValueKind.Enum;
        }

        if (type == typeof(bool))
        {
            return DataValueKind.Boolean;
        }

        if (type == typeof(Guid))
        {
            return DataValueKind.Guid;
        }

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly) ||
            type == typeof(TimeOnly))
        {
            return DataValueKind.Date;
        }

        return Type.GetTypeCode(type) switch
        {
            TypeCode.Byte or TypeCode.SByte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 or
                TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or TypeCode.Single or TypeCode.Double or
                TypeCode.Decimal => DataValueKind.Number,
            _ => DataValueKind.Other
        };
    }
}

/// <summary>One page of data returned by a source.</summary>
/// <param name="Items">The rows of the page.</param>
/// <param name="TotalCount">The total number of matching rows, or null when unknown (the pager then shows next/previous only).</param>
/// <param name="NextCursor">For cursor paging: the cursor of the next page, or null when this is the last page.</param>
public sealed record DataPage<TItem>(IReadOnlyList<TItem> Items, int? TotalCount, string? NextCursor = null)
{
    /// <summary>An empty page with a total of zero.</summary>
    public static DataPage<TItem> Empty { get; } = new([], 0);
}
