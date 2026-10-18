using GraphQLTestClient;
using TabBlazor.Tests.Components.DataViews;

namespace TabBlazor.Linq2GraphQL.Tests;

/// <summary>The shared source tests against HotChocolate, cursor paging with a total.</summary>
public class CursorSourceTests(GraphQLServerFixture server)
    : DataViewSourceTests<Linq2GraphQLSource<TestItem, TestItemFilterInput, TestItemSortInput>, TestItem>, IClassFixture<GraphQLServerFixture>
{
    // HotChocolate filters an IQueryable in memory with ordinal string comparisons.
    protected override bool SearchIsCaseInsensitive => false;

    protected override Linq2GraphQLSource<TestItem, TestItemFilterInput, TestItemSortInput> CreateSource(IReadOnlyList<SourceTestItem> items) =>
        Linq2GraphQLSource.Cursor<TestItem, TestItemFilterInput, TestItemSortInput>(
            (first, after, where, order) => server.Client.Query
                .Items(first: first, after: after, where: where, order: order)
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
