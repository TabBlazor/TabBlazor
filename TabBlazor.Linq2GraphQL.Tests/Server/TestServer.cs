using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

// A HotChocolate server shaped like Linq2GraphQL's test server ([UsePaging], [UseFiltering], [UseSorting] over an
// IQueryable), seeded with the rows of TabBlazor's shared source tests. The client in ../Generated was generated
// from it with the Linq2GraphQL generator (see ../README.md).
namespace TabBlazor.Linq2GraphQL.Tests.Server
{
    public enum TestStatus
    {
        Open,
        OnHold,
        Closed
    }

    public class TestCustomer
    {
        public string Name { get; set; } = "";
    }

    public class TestItem
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public TestCustomer? Customer { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public DateTime Date { get; set; }
        public bool Active { get; set; }
        public TestStatus Status { get; set; }
        public Guid Code { get; set; }
        public string? Note { get; set; }
    }

    public class Query
    {
        [UsePaging(IncludeTotalCount = true, MaxPageSize = 200)]
        [UseFiltering]
        [UseSorting]
        public IQueryable<TestItem> GetItems() => TestData.Items.AsQueryable();

        [UsePaging(IncludeTotalCount = false, MaxPageSize = 200)]
        [UseFiltering]
        [UseSorting]
        public IQueryable<TestItem> GetItemsNoTotalCount() => TestData.Items.AsQueryable();

        [UseOffsetPaging(IncludeTotalCount = true, MaxPageSize = 200)]
        [UseFiltering]
        [UseSorting]
        public IQueryable<TestItem> GetItemsOffset() => TestData.Items.AsQueryable();
    }

    public static class TestData
    {
        public static List<TestItem> Items { get; set; } = [];
    }

    public static class TestServerSetup
    {
        public static void AddTestGraphQLServer(this IServiceCollection services) =>
            services
                .AddGraphQLServer()
                .ModifyCostOptions(options => options.EnforceCostLimits = false)
                .AddQueryType<Query>()
                .AddFiltering()
                .AddSorting();

        public static void MapTestGraphQLServer(this WebApplication app) => app.MapGraphQL();
    }
}
