# TabBlazor.QuickTable.EntityFramework

Entity Framework Core integration for [TabBlazor](https://www.nuget.org/packages/TabBlazor/) QuickTable. Adds an async query executor so QuickTable can page, sort, and filter `IQueryable<T>` sources backed by EF Core without buffering the full result set.

**[Docs](https://tabblazor.com)** · **[GitHub](https://github.com/TabBlazor/TabBlazor)**

## Install

```bash
dotnet add package TabBlazor.QuickTable.EntityFramework
```

## Setup

Register the EF adapter alongside `AddTabBlazor`:

```csharp
builder.Services.AddTabBlazor();
builder.Services.AddQuickTableEntityFrameworkAdapter();
```

Now `QuickTable` will execute `IQueryable<T>` items providers using EF Core's async APIs (`ToListAsync`, `CountAsync`, etc.).

## Example

```razor
<QuickTable Items="db.Customers" Pagination="pagination">
    <PropertyColumn Property="c => c.Name" Sortable="true" />
    <PropertyColumn Property="c => c.Email" />
</QuickTable>
```

## License

MIT
