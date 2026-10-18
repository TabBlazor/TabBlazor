namespace TabBlazor.Linq2GraphQL;

/// <summary>What a server supports for one GraphQL path: sorting, and which filter operators (by GraphQL name).</summary>
/// <param name="Sortable">The path ends in a sort direction on the sort input.</param>
/// <param name="FilterOperators">The operator names of the path's leaf filter input (empty when it cannot be filtered).</param>
/// <param name="ValueType">The CLR type of the leaf's equality operator, used to convert filter and search values.</param>
public sealed record FieldCapabilities(bool Sortable, IReadOnlyList<string> FilterOperators, Type? ValueType)
{
    public static FieldCapabilities None { get; } = new(false, [], null);
}

/// <summary>A DataView column as GraphQL paths.</summary>
public sealed record GraphQLColumn(
    string Id,
    IReadOnlyList<string> SortPaths,
    IReadOnlyList<string> FilterPaths,
    FilterPathMode FilterMode,
    DataValueKind Kind);

/// <summary>The request's columns as GraphQL paths, by column id.</summary>
public sealed class FieldPaths(IReadOnlyDictionary<string, GraphQLColumn> columns)
{
    public IReadOnlyDictionary<string, GraphQLColumn> Columns { get; } = columns;

    public GraphQLColumn Get(string id) => Columns.TryGetValue(id, out var column)
        ? column
        : throw new InvalidOperationException($"The request refers to column '{id}', which is not in the request's columns.");
}

/// <summary>
/// Everything that depends on how a server does filtering, search and sorting. <see cref="HotChocolateConvention"/>
/// is the default; another server style gets its own class, or overrides one method.
/// </summary>
public interface IGraphQLConvention
{
    /// <summary>What the server supports for a path (from the generated input classes).</summary>
    FieldCapabilities Describe(Type filterType, Type sortType, string path);

    /// <summary>
    /// The request's filters and search as the generated filter input, or null when there are none. Returns
    /// <see cref="HotChocolateConvention.MatchesNothing"/> when no row can match (a search word no column can match).
    /// </summary>
    object? BuildFilter(Type filterType, IReadOnlyList<FilterSpec> filters, SearchSpec? search, FieldPaths paths);

    /// <summary>The request's sorts as a list of the generated sort input (one field per item), or null.</summary>
    object? BuildSort(Type sortType, IReadOnlyList<SortSpec> sorts, FieldPaths paths);
}
