# TabBlazor.Linq2GraphQL

A [Linq2GraphQL](https://linq2graphql.com) source for TabBlazor's `DataView`: paging, sorting, word-based
search and column filters go to a HotChocolate server (`[UsePaging]`/`[UseOffsetPaging]`, `[UseFiltering]`,
`[UseSorting]`) through your generated Linq2GraphQL client. Linq2GraphQL itself needs no changes.

```csharp
var orders = Linq2GraphQLSource.Cursor<Order, OrderFilterInput, OrderSortInput>(
    (first, after, where, order) => client.Query
        .Orders(first: first, after: after, where: where, order: order)
        .Include(c => c.Nodes.Select(o => o.Customer)));
```

```razor
<DataView TItem="Order" Source="orders" KeySelector="o => o.OrderId">
    <DataColumn Value="o => o.OrderNo" Sortable Searchable />
    <DataColumn Value="o => o.Customer.Name" Sortable Searchable Filterable />
    <DataColumn Value="o => o.Status" Filterable />
</DataView>
```

- **Rows** are the generated types. The adapter includes the nodes (their scalar fields), the total count and the
  page info; include nested objects your columns show yourself, as above.
- **Paths:** each column's member paths (`o => o.Customer.Name` → `customer.name`) are turned into GraphQL paths
  through the generated `[GraphQLMember]` names and checked against the generated filter and sort inputs on first
  use. A sortable, filterable or searchable column whose path does not exist is a clear error before any query.
- **Calculated columns** need nothing extra when they read members (`$"{o.FirstName} {o.LastName}"` sorts and
  searches by its parts). Arithmetic values need `SortBy`/`FilterBy`, or a server field:
  `orders.MapColumn("total", sortPath: "total", filterPath: "total")`.
- **Offset paging:** `Linq2GraphQLSource.Offset<...>((skip, take, where, order) => ...)` for `[UseOffsetPaging]`.
- **Server rules** live in one class, `HotChocolateConvention`. Override operator names
  (`orders.Convention(new HotChocolateConvention { Contains = "icontains" })`) or derive from it and override
  `AddSearch`, `AddFilter` or `BuildSort`.
- HotChocolate's `contains` follows the database collation, so search can be case-sensitive.
