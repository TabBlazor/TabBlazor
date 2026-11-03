# TabBlazor.Linq2GraphQL

A [Linq2GraphQL](https://linq2graphql.com) source for TabBlazor's `DataView`: paging, sorting, word-based
search and column filters go to a HotChocolate server (`[UsePaging]`/`[UseOffsetPaging]`, `[UseFiltering]`,
`[UseSorting]`) through your generated Linq2GraphQL client. Linq2GraphQL itself needs no changes.

```csharp
var orders = Linq2GraphQLSource.Cursor<Order, OrderFilterInput, OrderSortInput>(
    (first, after, where, order) => client.Query
        .Orders(first: first, after: after!, where: where, order: order!)
        .Include(c => c.Nodes.Select(o => o.Customer)));
```

The `!` keep a nullable-enabled app free of warnings: the page function passes `string?` and `List<TSort>?`
(null on the first page and when nothing is sorted), but a client generated without the generator's nullable
option (`-nu`) declares `string after = null` and `List<TSort> order = null`, which gives CS8604. Null is what those
parameters default to, so the `!` only silence the warning. With a client generated with `-nu` they are not needed;
add `!` to any other argument the compiler warns about in the same way.

```razor
<DataView TItem="Order" Source="orders" KeySelector="o => o.OrderId">
    <DataColumn Value="o => o.OrderNo" Sortable Searchable />
    <DataColumn Value="o => o.Customer.Name" Sortable Searchable Filterable />
    <DataColumn Value="o => o.Status" Filterable />
</DataView>
```

- **Rows** are the generated types. The adapter includes the nodes (their scalar fields), the total count and the
  page info; include nested objects your columns show yourself, as above. To fetch less, see
  [Fetching only the columns' fields](#fetching-only-the-columns-fields).
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
- **Enum names:** generated enums show their member names (`SapS4`) and cannot carry `[Display]`. Name them once
  with `EnumDisplay.Formatter = e => e is SystemKind.SapS4 ? "SAP S/4HANA" : null;` (cells, filter options, chips
  and editors), or per column with `Formatter`/`OptionText`, which enum filter options and chips also use.
  Members marked `[Browsable(false)]` or starting with `__` (the generator's `AddUnknownOption` adds `__Unknown` with
  both) are not offered as filter options.

## Fetching only the columns' fields

By default every row comes with all its scalar fields, whether a column shows them or not. `FetchColumnFieldsOnly()`
asks for just the fields the columns read: the members of each column's value (`o => o.Customer.Name`,
`$"{o.FirstName} {o.LastName}"`), its `SortBy` and `FilterBy` members, and the `KeySelector` when it is a member
path. Nested fields come along (`customer { name }`), so the page function no longer needs to include them.

```csharp
var orders = Linq2GraphQLSource.Cursor<Order, OrderFilterInput, OrderSortInput>(
        (first, after, where, order) => client.Query
            .Orders(first: first, after: after!, where: where, order: order!)
            .Include(c => c.Nodes.Select(o => o.Email))) // read by a cell template
    .FetchColumnFieldsOnly();
```

It is off by default because a DataView can read fields that no column names, and those come back empty:

- cell `Template`s, `ListItemTemplate`, `DetailsTemplate`, row actions, `CellColor`/`CellClass`/`CellStyle`;
- edit forms, which would also save the missing fields back as empty values;
- a `KeySelector` that is not a member path (`o => new { o.A, o.B }`), which would give every row the same key.

Include such fields in the page function as above. A column that uses the whole row in a way that cannot be traced
to fields (`o => Describe(o)`) makes the adapter fetch every scalar field, as without the option.

## HotChocolate's cost limit

Linq2GraphQL sends `where` and `order` as variables, and HotChocolate costs a variable filter input as the worst
case (every field and operator used), not by the filter actually sent. A filtered page of a wide type easily costs
a few thousand against the default `MaxFieldCost` of 1,000, and the server rejects every query with `HC0047`
("The maximum allowed field cost was exceeded"); the same filter written inline would cost a few dozen.

Turn the limit off, or raise it, on the server:

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyCostOptions(o => o.EnforceCostLimits = false) // or: o.MaxFieldCost = 10_000
    .AddFiltering()
    .AddSorting();
```

The adapter recognises the error and throws a `GraphQLCostLimitException` (with the reported `FieldCost` and
`MaxFieldCost`) whose message names these settings, so the DataView's error state says what to change.
