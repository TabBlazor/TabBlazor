using Linq2GraphQL.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TestItemsClient = GraphQLTestClient.TestItemsClient;
using TabBlazor.Linq2GraphQL.Tests.Server;
using TabBlazor.Tests.Components.DataViews;

namespace TabBlazor.Linq2GraphQL.Tests;

/// <summary>Runs the HotChocolate test server in-process, seeded with the shared source tests' rows.</summary>
public class GraphQLServerFixture : IAsyncLifetime
{
    private WebApplication? app;

    /// <summary>HotChocolate's field cost limit, or null to turn cost limits off.</summary>
    protected virtual double? MaxFieldCost => null;

    public TestItemsClient Client { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        TestData.Items = SourceTestItem.Seed().Select(i => new Server.TestItem
        {
            Id = i.Id,
            FirstName = i.FirstName,
            LastName = i.LastName,
            Customer = i.Customer == null ? null : new Server.TestCustomer { Name = i.Customer.Name },
            Quantity = i.Quantity,
            Price = i.Price,
            Date = i.Date,
            Active = i.Active,
            Status = (Server.TestStatus)(int)i.Status,
            Code = i.Code,
            Note = i.Note
        }).ToList();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddTestGraphQLServer(MaxFieldCost);
        app = builder.Build();
        app.MapTestGraphQLServer();
        await app.StartAsync();

        var http = app.GetTestClient();
        http.BaseAddress = new Uri("http://localhost/graphql/");
        var services = new ServiceCollection().AddMemoryCache().BuildServiceProvider();
        Client = new TestItemsClient(http, Options.Create(new GraphClientOptions()), services);
    }

    public async Task DisposeAsync()
    {
        if (app != null)
        {
            await app.DisposeAsync();
        }
    }
}

/// <summary>The test server with HotChocolate's cost limit on and set low, so filtered queries exceed it (HC0047).</summary>
public sealed class CostLimitedGraphQLServerFixture : GraphQLServerFixture
{
    protected override double? MaxFieldCost => 20;
}
