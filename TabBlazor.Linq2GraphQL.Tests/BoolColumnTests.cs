using TabBlazor.Tests.Components.DataViews;
using Package = GraphQLTestClient;
using Standalone = StandaloneTestClient;

namespace TabBlazor.Linq2GraphQL.Tests;

// HotChocolate filters bools with BooleanOperationFilterInput, which has only eq and neq. Filterable bool columns,
// top-level and nested, must be accepted on load and filter with eq, with package-based and standalone clients.

public class PackageBoolColumnTests(GraphQLServerFixture server)
    : BoolColumnTests<Package.TestItem, Package.TestItemFilterInput, Package.TestItemSortInput>, IClassFixture<GraphQLServerFixture>
{
    protected override DataColumnInfo Active { get; } =
        DataColumnInfoBuilder.Create<Package.TestItem, bool>(x => x.Active) with { Filterable = true };

    protected override DataColumnInfo Preferred { get; } =
        DataColumnInfoBuilder.Create<Package.TestItem, bool>(x => x.Customer!.Preferred) with { Filterable = true };

    protected override DataColumnInfo FirstName { get; } =
        DataColumnInfoBuilder.Create<Package.TestItem, string>(x => x.FirstName) with { Filterable = true };

    protected override Linq2GraphQLSource<Package.TestItem, Package.TestItemFilterInput, Package.TestItemSortInput> CreateSource() =>
        Linq2GraphQLSource.Cursor<Package.TestItem, Package.TestItemFilterInput, Package.TestItemSortInput>(
                (first, after, where, order) => server.Client.Query.Items(first: first, after: after, where: where, order: order));

    protected override int IdOf(Package.TestItem item) => item.Id;
}

public class StandaloneBoolColumnTests(GraphQLServerFixture server)
    : BoolColumnTests<Standalone.TestItem, Standalone.TestItemFilterInput, Standalone.TestItemSortInput>, IClassFixture<GraphQLServerFixture>
{
    protected override DataColumnInfo Active { get; } =
        DataColumnInfoBuilder.Create<Standalone.TestItem, bool>(x => x.Active) with { Filterable = true };

    protected override DataColumnInfo Preferred { get; } =
        DataColumnInfoBuilder.Create<Standalone.TestItem, bool>(x => x.Customer!.Preferred) with { Filterable = true };

    protected override DataColumnInfo FirstName { get; } =
        DataColumnInfoBuilder.Create<Standalone.TestItem, string>(x => x.FirstName) with { Filterable = true };

    protected override Linq2GraphQLSource<Standalone.TestItem, Standalone.TestItemFilterInput, Standalone.TestItemSortInput> CreateSource() =>
        Linq2GraphQLSource.Cursor<Standalone.TestItem, Standalone.TestItemFilterInput, Standalone.TestItemSortInput>(
                (first, after, where, order) => server.StandaloneClient.Query.Items(first: first, after: after, where: where, order: order));

    protected override int IdOf(Standalone.TestItem item) => item.Id;
}

public abstract class BoolColumnTests<TItem, TFilter, TSort>
    where TFilter : class, new()
    where TSort : class, new()
{
    private readonly IReadOnlyList<SourceTestItem> seed = SourceTestItem.Seed();

    protected abstract DataColumnInfo Active { get; }

    protected abstract DataColumnInfo Preferred { get; }

    protected abstract DataColumnInfo FirstName { get; }

    protected abstract Linq2GraphQLSource<TItem, TFilter, TSort> CreateSource();

    protected abstract int IdOf(TItem item);

    [Fact]
    public void Bool_operation_inputs_offer_eq_and_neq()
    {
        foreach (var path in new[] { "active", "customer.preferred" })
        {
            var capabilities = HotChocolateConvention.Default.Describe(typeof(TFilter), typeof(TSort), path);

            Assert.Equal(["eq", "neq"], capabilities.FilterOperators.Order());
            Assert.Equal(typeof(bool), Nullable.GetUnderlyingType(capabilities.ValueType!) ?? capabilities.ValueType);
        }
    }

    [Fact]
    public void Object_filter_inputs_are_not_operation_inputs() =>
        // customer is an object filter input (and, or, name, preferred), not a value to filter.
        Assert.Empty(HotChocolateConvention.Default.Describe(typeof(TFilter), typeof(TSort), "customer").FilterOperators);

    [Fact]
    public void Enum_operation_inputs_keep_their_operators() =>
        Assert.Equal(["eq", "in", "neq", "nin"],
            HotChocolateConvention.Default.Describe(typeof(TFilter), typeof(TSort), "status").FilterOperators.Order());

    [Fact]
    public async Task Filterable_bool_columns_load_unfiltered() =>
        Assert.Equal(seed.Select(x => x.Id).Order(), await Ids());

    [Fact]
    public async Task Top_level_bool_filters_with_eq()
    {
        Assert.Equal(seed.Where(x => x.Active).Select(x => x.Id), await Ids(new FilterSpec(Active.Id, FilterOperator.Equals, true)));
        Assert.Equal(seed.Where(x => !x.Active).Select(x => x.Id), await Ids(new FilterSpec(Active.Id, FilterOperator.Equals, false)));
    }

    [Fact]
    public async Task Nested_bool_filters_with_eq()
    {
        Assert.Equal([1, 3], await Ids(new FilterSpec(Preferred.Id, FilterOperator.Equals, true)));
        // A row without a customer (row 5) matches neither.
        Assert.Equal(seed.Where(x => x.Customer != null && !GraphQLServerFixture.IsPreferred(x.Customer.Name)).Select(x => x.Id),
            await Ids(new FilterSpec(Preferred.Id, FilterOperator.Equals, false)));
    }

    [Fact]
    public async Task Bool_filters_combine_with_other_filters() =>
        Assert.Equal([1], await Ids(
            new FilterSpec(Preferred.Id, FilterOperator.Equals, true),
            new FilterSpec(FirstName.Id, FilterOperator.Equals, "Ada")));

    private async Task<List<int>> Ids(params FilterSpec[] filters)
    {
        var request = new DataRequest(PageRequest.Cursor(null, 100), [], null, filters, [FirstName, Active, Preferred]);
        var page = await CreateSource().GetPageAsync(request, CancellationToken.None);
        return page.Items.Select(IdOf).Order().ToList();
    }
}
