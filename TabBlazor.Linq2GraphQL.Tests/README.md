# TabBlazor.Linq2GraphQL.Tests

End-to-end tests of the Linq2GraphQL adapter. `Server/TestServer.cs` is a HotChocolate server shaped like
Linq2GraphQL's own test server (`[UsePaging]`, `[UseOffsetPaging]`, `[UseFiltering]`, `[UseSorting]` over an
`IQueryable`), hosted in-process and seeded with the rows of the shared source tests, which run here unchanged
(`Shared/DataViewSourceTests.cs` is linked from `TabBlazor.Tests`).

`Generated/` is the client produced by the Linq2GraphQL generator from `Server/schema.graphql`. After changing the
server, refresh both:

1. Run the server on a local port (any ASP.NET Core host calling `AddTestGraphQLServer()` and
   `MapTestGraphQLServer()`) and save `http://127.0.0.1:<port>/graphql/?sdl` as `Server/schema.graphql`.
2. Generate the client from the file (the version must match the `Linq2GraphQL.Client` package):

```bash
dotnet tool install --global Linq2GraphQL.Generator --version 1.0.67-prerelease
```

```bash
Linq2GraphQL -sc=Server/schema.graphql -c=TestItemsClient -n=GraphQLTestClient -o=Generated -nu
```

The client's namespace must not start with `TabBlazor.`: the generated code refers to `Linq2GraphQL.Client…`
without `global::`, which would resolve to this adapter's `TabBlazor.Linq2GraphQL` namespace.
