using StandaloneTestClient;
using TabBlazor.Tests.Components.DataViews;
using Runtime = StandaloneTestClient.Runtime;

namespace TabBlazor.Linq2GraphQL.Tests;

// A client generated with --standalone carries its own copy of the Linq2GraphQL runtime (StandaloneTestClient.Runtime):
// its inputs derive from that copy's GraphInputBase, its members carry that copy's [GraphQLMember], and its
// queries and exceptions are that copy's types. The adapter must treat them as it treats the package's.

/// <summary>The shared source tests through a standalone client, cursor paging with a total.</summary>
public class StandaloneCursorSourceTests(GraphQLServerFixture server)
    : DataViewSourceTests<Linq2GraphQLSource<TestItem, TestItemFilterInput, TestItemSortInput>, TestItem>, IClassFixture<GraphQLServerFixture>
{
    protected override bool SearchIsCaseInsensitive => false;

    protected override Linq2GraphQLSource<TestItem, TestItemFilterInput, TestItemSortInput> CreateSource(IReadOnlyList<SourceTestItem> items) =>
        Linq2GraphQLSource.Cursor<TestItem, TestItemFilterInput, TestItemSortInput>(
            (first, after, where, order) => server.StandaloneClient.Query
                .Items(first: first, after: after, where: where, order: order)
                .Include(c => c!.Nodes!.Select(i => i.Customer)));

    protected override int IdOf(TestItem item) => item.Id;

    [Fact]
    public void Inputs_do_not_derive_from_the_Linq2GraphQL_Client_package()
    {
        Assert.True(typeof(Runtime.GraphInputBase).IsAssignableFrom(typeof(TestItemFilterInput)));
        Assert.False(typeof(global::Linq2GraphQL.Client.GraphInputBase).IsAssignableFrom(typeof(TestItemFilterInput)));
        Assert.False(typeof(global::Linq2GraphQL.Client.GraphInputBase).IsAssignableFrom(typeof(TestItemSortInput)));
    }
}

/// <summary>The shared source tests through a standalone client, offset paging.</summary>
public class StandaloneOffsetSourceTests(GraphQLServerFixture server)
    : DataViewSourceTests<Linq2GraphQLSource<TestItem, TestItemFilterInput, TestItemSortInput>, TestItem>, IClassFixture<GraphQLServerFixture>
{
    protected override bool SearchIsCaseInsensitive => false;

    protected override Linq2GraphQLSource<TestItem, TestItemFilterInput, TestItemSortInput> CreateSource(IReadOnlyList<SourceTestItem> items) =>
        Linq2GraphQLSource.Offset<TestItem, TestItemFilterInput, TestItemSortInput>(
            (skip, take, where, order) => server.StandaloneClient.Query
                .ItemsOffset(skip: skip, take: take, where: where, order: order)
                .Include(c => c!.Items!.Select(i => i.Customer)));

    protected override int IdOf(TestItem item) => item.Id;
}

/// <summary>The shared source tests through a standalone client with only the columns' fields fetched.</summary>
public class StandaloneColumnFieldsSourceTests(GraphQLServerFixture server)
    : DataViewSourceTests<Linq2GraphQLSource<TestItem, TestItemFilterInput, TestItemSortInput>, TestItem>, IClassFixture<GraphQLServerFixture>
{
    protected override bool SearchIsCaseInsensitive => false;

    protected override Linq2GraphQLSource<TestItem, TestItemFilterInput, TestItemSortInput> CreateSource(IReadOnlyList<SourceTestItem> items) =>
        Linq2GraphQLSource.Cursor<TestItem, TestItemFilterInput, TestItemSortInput>(
            (first, after, where, order) => server.StandaloneClient.Query.Items(first: first, after: after, where: where, order: order))
            .FetchColumnFieldsOnly();

    protected override int IdOf(TestItem item) => item.Id;
}

public class StandaloneCostLimitTests(CostLimitedGraphQLServerFixture server) : IClassFixture<CostLimitedGraphQLServerFixture>
{
    private readonly SourceTestColumns<TestItem> columns = new();

    [Fact]
    public async Task A_query_over_the_servers_cost_limit_is_a_clear_error()
    {
        var source = Linq2GraphQLSource.Cursor<TestItem, TestItemFilterInput, TestItemSortInput>(
            (first, after, where, order) => server.StandaloneClient.Query.Items(first: first, after: after, where: where, order: order));
        var request = new DataRequest(PageRequest.Cursor(null, 10), [], null,
            [new FilterSpec(columns.FirstName.Id, FilterOperator.Contains, "a")], columns.All);

        var error = await Assert.ThrowsAsync<GraphQLCostLimitException>(() => source.GetPageAsync(request, CancellationToken.None));

        Assert.Equal(20, error.MaxFieldCost);
        Assert.True(error.FieldCost > 20);
        Assert.IsType<Runtime.GraphQueryRequestException>(error.InnerException);
    }

    [Fact]
    public void Cost_errors_in_parsed_GraphQL_errors_are_recognised()
    {
        var errors = new[]
        {
            new Runtime.GraphQueryError
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

        var error = GraphQLCostLimitException.From(new Runtime.GraphQueryExecutionException(errors, "query", []));

        Assert.NotNull(error);
        Assert.Equal(4200, error.FieldCost);
        Assert.Equal(1000, error.MaxFieldCost);
    }

    [Fact]
    public void Other_errors_are_not_cost_errors()
    {
        Assert.Null(GraphQLCostLimitException.From(new Runtime.GraphQueryRequestException("Http error! Status code BadRequest", "query", [])));
        Assert.Null(GraphQLCostLimitException.From(new Runtime.GraphQueryExecutionException([new Runtime.GraphQueryError { Message = "other" }], "query", [])));
    }
}

public class InputTypeTests
{
    private sealed class NotAnInput;

    [Fact]
    public void Filter_and_sort_types_must_be_generated_inputs()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            Linq2GraphQLSource.Cursor<TestItem, NotAnInput, TestItemSortInput>((_, _, _, _) => new object()));

        Assert.Contains(nameof(NotAnInput), error.Message);
    }

    [Fact]
    public void A_GraphInputBase_outside_a_Linq2GraphQL_runtime_namespace_is_not_an_input() =>
        Assert.Throws<ArgumentException>(() =>
            Linq2GraphQLSource.Cursor<TestItem, Unrelated.LookalikeFilterInput, TestItemSortInput>((_, _, _, _) => new object()));
}
