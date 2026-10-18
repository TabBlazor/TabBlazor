using System.Diagnostics.CodeAnalysis;
using Linq2GraphQL.Client;

namespace TabBlazor.Linq2GraphQL;

/// <summary>
/// Creates DataView sources over generated Linq2GraphQL clients for HotChocolate servers with
/// <c>[UsePaging]</c>/<c>[UseOffsetPaging]</c>, <c>[UseFiltering]</c> and <c>[UseSorting]</c>.
/// </summary>
/// <example>
/// <code>
/// var orders = Linq2GraphQLSource.Cursor&lt;Order, OrderFilterInput, OrderSortInput&gt;(
///     (first, after, where, order) => client.Query
///         .Orders(first: first, after: after, where: where, order: order)
///         .Include(c => c.Nodes.Select(o => o.Customer)));
/// </code>
/// </example>
public static class Linq2GraphQLSource
{
    /// <summary>A cursor-paged source. The function gets <c>first</c>, <c>after</c>, the filter and the sort, and returns the query.</summary>
    public static Linq2GraphQLSource<TNode, TFilter, TSort> Cursor<TNode,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TFilter,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TSort>(
        Func<int?, string?, TFilter?, List<TSort>?, object> query)
        where TFilter : GraphInputBase, new()
        where TSort : GraphInputBase, new() =>
        new(PagingMode.Cursor, (page, where, order) => query(page.First, page.After, where, order));

    /// <summary>An offset-paged source. The function gets <c>skip</c>, <c>take</c>, the filter and the sort, and returns the query.</summary>
    public static Linq2GraphQLSource<TNode, TFilter, TSort> Offset<TNode,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TFilter,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TSort>(
        Func<int?, int?, TFilter?, List<TSort>?, object> query)
        where TFilter : GraphInputBase, new()
        where TSort : GraphInputBase, new() =>
        new(PagingMode.Offset, (page, where, order) => query(page.Skip, page.Take, where, order));
}

/// <summary>
/// A DataView source over a generated Linq2GraphQL client. Column member paths are turned into GraphQL paths through
/// the generated types' <c>[GraphQLMember]</c> names; filters, search and sorts are built as the generated input types
/// by an <see cref="IGraphQLConvention"/> (HotChocolate's by default). Paths are checked on first use, so a wrong
/// path is a clear error before any query is sent.
/// </summary>
public sealed class Linq2GraphQLSource<TNode, TFilter, TSort> : IDataViewSource<TNode>
    where TFilter : GraphInputBase, new()
    where TSort : GraphInputBase, new()
{
    private readonly Func<PageRequest, TFilter?, List<TSort>?, object> query;
    private readonly Dictionary<string, (string? SortPath, string? FilterPath)> mappedColumns = [];
    private IGraphQLConvention convention = HotChocolateConvention.Default;
    private IReadOnlyList<DataColumnInfo>? checkedColumns;
    private FieldPaths? fieldPaths;

    internal Linq2GraphQLSource(PagingMode pagingMode, Func<PageRequest, TFilter?, List<TSort>?, object> query)
    {
        PagingMode = pagingMode;
        this.query = query;
    }

    public PagingMode PagingMode { get; }

    /// <summary>Uses another server convention, for example <c>new HotChocolateConvention { Contains = "icontains" }</c>.</summary>
    public Linq2GraphQLSource<TNode, TFilter, TSort> Convention(IGraphQLConvention value)
    {
        convention = value ?? throw new ArgumentNullException(nameof(value));
        checkedColumns = null;
        return this;
    }

    /// <summary>
    /// Maps a column to GraphQL paths of its own, for columns the server computes (for example a <c>fullName</c>
    /// field). Paths are GraphQL names, such as <c>customer.fullName</c>.
    /// </summary>
    public Linq2GraphQLSource<TNode, TFilter, TSort> MapColumn(string columnId, string? sortPath = null, string? filterPath = null)
    {
        mappedColumns[columnId] = (sortPath, filterPath);
        checkedColumns = null;
        return this;
    }

    public async Task<DataPage<TNode>> GetPageAsync(DataRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var paths = ResolvePaths(request);

        var filter = convention.BuildFilter(typeof(TFilter), request.Filters, request.Search, paths);
        if (ReferenceEquals(filter, HotChocolateConvention.MatchesNothing))
        {
            return new DataPage<TNode>([], 0);
        }

        var sort = (List<TSort>?)convention.BuildSort(typeof(TSort), request.Sorts, paths);
        var graphQuery = query(request.Page, (TFilter?)filter, sort)
                         ?? throw new InvalidOperationException("The page function returned no query.");
        var result = await GraphQueryRunner.ExecuteAsync<TNode>(graphQuery, cancellationToken).ConfigureAwait(false);

        var nextCursor = PagingMode == PagingMode.Cursor && result.HasNextPage ? result.EndCursor : null;
        return new DataPage<TNode>(result.Items, result.TotalCount, nextCursor);
    }

    // Column paths become GraphQL paths, checked against the generated inputs once per set of columns.
    private FieldPaths ResolvePaths(DataRequest request)
    {
        if (ReferenceEquals(request.Columns, checkedColumns) && fieldPaths != null)
        {
            // Columns used by this request were checked with the same column list.
            CheckUsed(request, fieldPaths);
            return fieldPaths;
        }

        var problems = new List<string>();
        var columns = new Dictionary<string, GraphQLColumn>(StringComparer.Ordinal);
        foreach (var info in request.Columns)
        {
            mappedColumns.TryGetValue(info.Id, out var mapped);
            var sortPaths = mapped.SortPath != null ? [mapped.SortPath] : MapPaths(info.SortPaths);
            var filterPaths = mapped.FilterPath != null ? [mapped.FilterPath] : MapPaths(info.FilterPaths);
            var filterMode = mapped.FilterPath != null ? FilterPathMode.Exact : info.FilterMode;
            columns[info.Id] = new GraphQLColumn(info.Id, sortPaths ?? [], filterPaths ?? [], filterMode, info.Kind);

            if (info.Sortable)
            {
                CheckSort(info, sortPaths, problems);
            }

            if (info.Filterable || info.Searchable)
            {
                CheckFilter(info, filterPaths, problems);
            }
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                $"TabBlazor.Linq2GraphQL cannot use these {typeof(TNode).Name} columns: " + string.Join(" ", problems));
        }

        fieldPaths = new FieldPaths(columns);
        checkedColumns = request.Columns;
        CheckUsed(request, fieldPaths);
        return fieldPaths;
    }

    // Columns that are only used by this request (the key sort, a sort on a column not marked sortable).
    private void CheckUsed(DataRequest request, FieldPaths paths)
    {
        var problems = new List<string>();
        foreach (var sort in request.Sorts)
        {
            var info = request.GetColumn(sort.Column);
            if (!info.Sortable)
            {
                CheckSort(info, paths.Get(info.Id).SortPaths, problems);
            }
        }

        foreach (var filter in request.Filters)
        {
            var info = request.GetColumn(filter.Column);
            if (!info.Filterable && !info.Searchable)
            {
                CheckFilter(info, paths.Get(info.Id).FilterPaths, problems);
            }
        }

        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                $"TabBlazor.Linq2GraphQL cannot use these {typeof(TNode).Name} columns: " + string.Join(" ", problems));
        }
    }

    private void CheckSort(DataColumnInfo info, IReadOnlyList<string>? paths, List<string> problems)
    {
        if (paths == null || paths.Count == 0)
        {
            problems.Add($"'{info.Id}' cannot be sorted: it has no member path; set SortBy or call MapColumn.");
            return;
        }

        foreach (var path in paths.Where(p => !convention.Describe(typeof(TFilter), typeof(TSort), p).Sortable))
        {
            problems.Add($"'{info.Id}' cannot be sorted: '{path}' is not sortable on {typeof(TSort).Name}.");
        }
    }

    private void CheckFilter(DataColumnInfo info, IReadOnlyList<string>? paths, List<string> problems)
    {
        if (paths == null || paths.Count == 0)
        {
            problems.Add($"'{info.Id}' cannot be filtered or searched: it has no member path; set FilterBy or call MapColumn.");
            return;
        }

        foreach (var path in paths.Where(p => convention.Describe(typeof(TFilter), typeof(TSort), p).FilterOperators.Count == 0))
        {
            problems.Add($"'{info.Id}' cannot be filtered or searched: '{path}' is not filterable on {typeof(TFilter).Name}.");
        }
    }

    // Null when a member path does not exist on the row type.
    private static IReadOnlyList<string>? MapPaths(IReadOnlyList<string> memberPaths)
    {
        var result = new List<string>(memberPaths.Count);
        foreach (var memberPath in memberPaths)
        {
            var graphQLPath = GraphQLMembers.ToGraphQLPath(typeof(TNode), memberPath);
            if (graphQLPath == null)
            {
                return null;
            }

            result.Add(graphQLPath);
        }

        return result;
    }
}
