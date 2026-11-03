using GraphQLTestClient;
using TabBlazor.Tests.Components.DataViews;

namespace TabBlazor.Linq2GraphQL.Tests;

/// <summary>The shared source tests against HotChocolate, cursor paging with a total.</summary>
public class CursorSourceTests(GraphQLServerFixture server)
    : DataViewSourceTests<Linq2GraphQLSource<TestItem, TestItemFilterInput, TestItemSortInput>, TestItem>, IClassFixture<GraphQLServerFixture>
{
    // HotChocolate filters an IQueryable in memory with ordinal string comparisons.
    protected override bool SearchIsCaseInsensitive => false;

    // Written as in the README, with the '!' a client generated without nullable annotations needs.
    protected override Linq2GraphQLSource<TestItem, TestItemFilterInput, TestItemSortInput> CreateSource(IReadOnlyList<SourceTestItem> items) =>
        Linq2GraphQLSource.Cursor<TestItem, TestItemFilterInput, TestItemSortInput>(
            (first, after, where, order) => server.Client.Query
                .Items(first: first, after: after!, where: where, order: order!)
                .Include(c => c!.Nodes!.Select(i => i.Customer)));

    protected override int IdOf(TestItem item) => item.Id;
}

/// <summary>The shared source tests against HotChocolate, cursor paging without a total.</summary>
public class CursorNoTotalSourceTests(GraphQLServerFixture server)
    : DataViewSourceTests<Linq2GraphQLSource<TestItem, TestItemFilterInput, TestItemSortInput>, TestItem>, IClassFixture<GraphQLServerFixture>
{
    protected override bool SearchIsCaseInsensitive => false;

    protected override Linq2GraphQLSource<TestItem, TestItemFilterInput, TestItemSortInput> CreateSource(IReadOnlyList<SourceTestItem> items) =>
        Linq2GraphQLSource.Cursor<TestItem, TestItemFilterInput, TestItemSortInput>(
            (first, after, where, order) => server.Client.Query
                .ItemsNoTotalCount(first: first, after: after, where: where, order: order)
                .Include(c => c!.Nodes!.Select(i => i.Customer)));

    protected override int IdOf(TestItem item) => item.Id;

    [Fact]
    public async Task Total_count_is_unknown()
    {
        var page = await Source.GetPageAsync(Request(FirstPage(5)), CancellationToken.None);

        Assert.Null(page.TotalCount);
        Assert.NotNull(page.NextCursor);
    }
}

/// <summary>The shared source tests against HotChocolate, offset paging.</summary>
public class OffsetSourceTests(GraphQLServerFixture server)
    : DataViewSourceTests<Linq2GraphQLSource<TestItem, TestItemFilterInput, TestItemSortInput>, TestItem>, IClassFixture<GraphQLServerFixture>
{
    protected override bool SearchIsCaseInsensitive => false;

    protected override Linq2GraphQLSource<TestItem, TestItemFilterInput, TestItemSortInput> CreateSource(IReadOnlyList<SourceTestItem> items) =>
        Linq2GraphQLSource.Offset<TestItem, TestItemFilterInput, TestItemSortInput>(
            (skip, take, where, order) => server.Client.Query
                .ItemsOffset(skip: skip, take: take, where: where, order: order)
                .Include(c => c!.Items!.Select(i => i.Customer)));

    protected override int IdOf(TestItem item) => item.Id;
}

/// <summary>The shared source tests with only the columns' fields fetched.</summary>
public class ColumnFieldsSourceTests(GraphQLServerFixture server)
    : DataViewSourceTests<Linq2GraphQLSource<TestItem, TestItemFilterInput, TestItemSortInput>, TestItem>, IClassFixture<GraphQLServerFixture>
{
    protected override bool SearchIsCaseInsensitive => false;

    protected override Linq2GraphQLSource<TestItem, TestItemFilterInput, TestItemSortInput> CreateSource(IReadOnlyList<SourceTestItem> items) =>
        Linq2GraphQLSource.Cursor<TestItem, TestItemFilterInput, TestItemSortInput>(
            (first, after, where, order) => server.Client.Query.Items(first: first, after: after, where: where, order: order))
            .FetchColumnFieldsOnly();

    protected override int IdOf(TestItem item) => item.Id;
}

public class ColumnFieldsTests(GraphQLServerFixture server) : IClassFixture<GraphQLServerFixture>
{
    private static readonly System.Linq.Expressions.Expression<Func<TestItem, string>> WholeRow = x => Describe(x);

    private Linq2GraphQLSource<TestItem, TestItemFilterInput, TestItemSortInput> CreateSource() =>
        Linq2GraphQLSource.Cursor<TestItem, TestItemFilterInput, TestItemSortInput>(
            (first, after, where, order) => server.Client.Query.Items(first: first, after: after, where: where, order: order));

    private static DataRequest Request(params DataColumnInfo[] columns) =>
        new(PageRequest.Cursor(null, 50), [], null, [], columns);

    private static string Describe(TestItem item) => item.FirstName;

    [Fact]
    public async Task Every_scalar_field_is_fetched_by_default()
    {
        var page = await CreateSource().GetPageAsync(
            Request(DataColumnInfoBuilder.Create<TestItem, string>(x => x.FirstName)), CancellationToken.None);

        var ada = page.Items.First(i => i.Id == 1);
        Assert.Equal("Lovelace", ada.LastName);
        Assert.Equal(2026, ada.Quantity);
        Assert.Null(ada.Customer);
    }

    [Fact]
    public async Task Only_the_fields_the_columns_read_are_fetched()
    {
        var request = Request(
            DataColumnInfoBuilder.Create<TestItem, int>(x => x.Id),
            DataColumnInfoBuilder.Create<TestItem, string>(x => x.FirstName),
            DataColumnInfoBuilder.Create<TestItem, string>(x => x.Customer!.Name));

        var page = await CreateSource().FetchColumnFieldsOnly().GetPageAsync(request, CancellationToken.None);

        var ada = page.Items.First(i => i.Id == 1);
        Assert.Equal("Ada", ada.FirstName);
        Assert.Equal("Odin AB", ada.Customer!.Name);
        Assert.Null(ada.LastName);
        Assert.Equal(0, ada.Quantity);
    }

    [Fact]
    public async Task Calculated_values_SortBy_and_FilterBy_members_are_fetched()
    {
        var request = Request(
            DataColumnInfoBuilder.Create<TestItem, int>(x => x.Id),
            DataColumnInfoBuilder.Create<TestItem, string>(x => $"{x.FirstName} {x.Note}",
                sortBy: (System.Linq.Expressions.Expression<Func<TestItem, object>>)(x => x.Quantity),
                filterBy: (System.Linq.Expressions.Expression<Func<TestItem, object>>)(x => x.Price)));

        var page = await CreateSource().FetchColumnFieldsOnly().GetPageAsync(request, CancellationToken.None);

        var ada = page.Items.First(i => i.Id == 1);
        Assert.Equal("Ada", ada.FirstName);
        Assert.Equal("vip", ada.Note);
        Assert.Equal(2026, ada.Quantity);
        Assert.Equal(10.50m, ada.Price);
        Assert.Null(ada.LastName);
    }

    [Fact]
    public async Task A_column_that_uses_the_whole_row_fetches_every_field()
    {
        var request = Request(
            DataColumnInfoBuilder.Create<TestItem, int>(x => x.Id),
            DataColumnInfoBuilder.Create(typeof(string), WholeRow, "description"));

        var page = await CreateSource().FetchColumnFieldsOnly().GetPageAsync(request, CancellationToken.None);

        Assert.Equal("Lovelace", page.Items.First(i => i.Id == 1).LastName);
    }

    [Fact]
    public async Task The_page_function_can_include_more_fields()
    {
        var source = Linq2GraphQLSource.Cursor<TestItem, TestItemFilterInput, TestItemSortInput>(
                (first, after, where, order) => server.Client.Query
                    .Items(first: first, after: after, where: where, order: order)
                    .Include(c => c!.Nodes!.Select(i => i.LastName)))
            .FetchColumnFieldsOnly();

        var page = await source.GetPageAsync(Request(DataColumnInfoBuilder.Create<TestItem, int>(x => x.Id)), CancellationToken.None);

        var ada = page.Items.First(i => i.Id == 1);
        Assert.Equal("Lovelace", ada.LastName);
        Assert.Null(ada.FirstName);
    }
}

public class Linq2GraphQLSourceBehaviourTests(GraphQLServerFixture server) : IClassFixture<GraphQLServerFixture>
{
    private readonly SourceTestColumns<TestItem> columns = new();

    private Linq2GraphQLSource<TestItem, TestItemFilterInput, TestItemSortInput> CreateSource() =>
        Linq2GraphQLSource.Cursor<TestItem, TestItemFilterInput, TestItemSortInput>(
            (first, after, where, order) => server.Client.Query.Items(first: first, after: after, where: where, order: order));

    [Fact]
    public async Task Columns_without_paths_are_reported_before_any_query()
    {
        var calculated = DataColumnInfoBuilder.Create<TestItem, decimal>(x => x.Quantity * x.Price) with { Sortable = true };
        var request = new DataRequest(PageRequest.Cursor(null, 10), [], null, [], [calculated]);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateSource().GetPageAsync(request, CancellationToken.None));

        Assert.Contains("'quantity-price' cannot be sorted", error.Message);
        Assert.Contains("MapColumn", error.Message);
    }

    [Fact]
    public async Task MapColumn_sends_the_server_field()
    {
        var calculated = DataColumnInfoBuilder.Create<TestItem, decimal>(x => x.Quantity * x.Price, id: "total") with { Sortable = true };
        var request = new DataRequest(PageRequest.Cursor(null, 3), [new SortSpec("total", true)], null, [], [calculated]);

        var page = await CreateSource().MapColumn("total", sortPath: "price").GetPageAsync(request, CancellationToken.None);

        Assert.Equal(5, page.Items[0].Id);
    }

    [Fact]
    public async Task A_search_word_no_column_can_match_returns_no_rows_without_asking()
    {
        var search = SearchSpec.Parse("notanumber", SearchMode.Words, [columns.Quantity.Id]);
        var request = new DataRequest(PageRequest.Cursor(null, 10), [], search, [], columns.All);

        var page = await CreateSource().GetPageAsync(request, CancellationToken.None);

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public void Filters_are_built_as_the_generated_inputs()
    {
        var paths = new FieldPaths(new Dictionary<string, GraphQLColumn>
        {
            ["customer.name"] = new("customer.name", ["customer.name"], ["customer.name"], FilterPathMode.Exact, DataValueKind.Text),
            ["quantity"] = new("quantity", ["quantity"], ["quantity"], FilterPathMode.Exact, DataValueKind.Number)
        });

        var filter = (TestItemFilterInput)HotChocolateConvention.Default.BuildFilter(typeof(TestItemFilterInput),
        [
            new FilterSpec("customer.name", FilterOperator.Contains, "Odi"),
            new FilterSpec("quantity", FilterOperator.Between, 5, 10)
        ], null, paths)!;

        Assert.Equal(2, filter.And!.Count);
        Assert.Equal("Odi", filter.And[0].Customer!.Name!.Contains);
        Assert.Equal(5, filter.And[1].Quantity!.Gte);
        Assert.Equal(10, filter.And[1].Quantity!.Lte);
    }

    [Fact]
    public void Sorts_use_the_generated_sort_enum()
    {
        var paths = new FieldPaths(new Dictionary<string, GraphQLColumn>
        {
            ["customer.name"] = new("customer.name", ["customer.name"], ["customer.name"], FilterPathMode.Exact, DataValueKind.Text)
        });

        var sorts = (List<TestItemSortInput>)HotChocolateConvention.Default.BuildSort(typeof(TestItemSortInput),
            [new SortSpec("customer.name", true)], paths)!;

        Assert.Equal(SortEnumType.Desc, sorts.Single().Customer!.Name);
    }

    [Fact]
    public void Describe_reads_operators_and_sortability_from_the_generated_classes()
    {
        var name = HotChocolateConvention.Default.Describe(typeof(TestItemFilterInput), typeof(TestItemSortInput), "customer.name");
        var missing = HotChocolateConvention.Default.Describe(typeof(TestItemFilterInput), typeof(TestItemSortInput), "customer.nope");

        Assert.True(name.Sortable);
        Assert.Contains("contains", name.FilterOperators);
        Assert.Equal(typeof(string), name.ValueType);
        Assert.False(missing.Sortable);
        Assert.Empty(missing.FilterOperators);
    }
}

public class CostLimitTests(CostLimitedGraphQLServerFixture server) : IClassFixture<CostLimitedGraphQLServerFixture>
{
    private readonly SourceTestColumns<TestItem> columns = new();

    [Fact]
    public async Task A_query_over_the_servers_cost_limit_is_a_clear_error()
    {
        var source = Linq2GraphQLSource.Cursor<TestItem, TestItemFilterInput, TestItemSortInput>(
            (first, after, where, order) => server.Client.Query.Items(first: first, after: after, where: where, order: order));
        var request = new DataRequest(PageRequest.Cursor(null, 10), [], null,
            [new FilterSpec(columns.FirstName.Id, FilterOperator.Contains, "a")], columns.All);

        var error = await Assert.ThrowsAsync<GraphQLCostLimitException>(() => source.GetPageAsync(request, CancellationToken.None));

        Assert.Equal(20, error.MaxFieldCost);
        Assert.True(error.FieldCost > 20);
        Assert.Contains("HC0047", error.Message);
        Assert.Contains("EnforceCostLimits", error.Message);
        Assert.IsType<global::Linq2GraphQL.Client.GraphQueryRequestException>(error.InnerException);
    }

    [Fact]
    public void Cost_errors_in_parsed_GraphQL_errors_are_recognised()
    {
        var errors = new[]
        {
            new global::Linq2GraphQL.Client.GraphQueryError
            {
                Message = "The maximum allowed field cost was exceeded.",
                Extensions = new()
                {
                    ["code"] = System.Text.Json.JsonDocument.Parse("\"HC0047\"").RootElement,
                    ["fieldCost"] = System.Text.Json.JsonDocument.Parse("4200").RootElement,
                    ["maxFieldCost"] = System.Text.Json.JsonDocument.Parse("1000").RootElement
                }
            }
        };

        var error = GraphQLCostLimitException.From(new global::Linq2GraphQL.Client.GraphQueryExecutionException(errors, "query", []));

        Assert.NotNull(error);
        Assert.Equal(4200, error.FieldCost);
        Assert.Equal(1000, error.MaxFieldCost);
    }

    [Fact]
    public void Other_errors_are_not_cost_errors() =>
        Assert.Null(GraphQLCostLimitException.From(new global::Linq2GraphQL.Client.GraphQueryRequestException("Http error! Status code BadRequest", "query", [])));
}
