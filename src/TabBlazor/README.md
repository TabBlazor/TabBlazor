# TabBlazor

Blazor component library built on the [Tabler](https://tabler.io/) UI framework.

- 90+ Razor components on Tabler 1.7: forms, cards, modals, offcanvas, navigation, dashboards
- **DataView** (preview): tables and lists with sorting, search, column filters, editing, selection, row actions, detail rows, virtualization and URL state
- **QuickTable** and **Table** for existing screens
- **Tabs** with deep links, preloading, keep-alive and a bottom tab bar on phones
- Form inputs with pluggable validation (DataAnnotations by default)
- Modal, offcanvas and toast services with async results
- Dark/light theme switch
- Minimal JavaScript interop

**[Live demo & docs](https://tabblazor.com)** · **[GitHub](https://github.com/TabBlazor/TabBlazor)**

## Install

Requires .NET 10.

```bash
dotnet add package TabBlazor
```

Register services in `Program.cs`:

```csharp
builder.Services.AddTabBlazor(options =>
{
    options.EnablePopper = true; // optional, dynamic tooltip/dropdown positioning
});
```

Add to `_Imports.razor`:

```razor
@using TabBlazor
```

Reference styles and scripts in `App.razor` (or `index.html` for WebAssembly):

```html
<link rel="stylesheet" href="https://unpkg.com/@tabler/core@1.7.0/dist/css/tabler.min.css" />
<link rel="stylesheet" href="_content/TabBlazor/css/tabblazor.min.css" />
<link rel="stylesheet" href="YourApp.styles.css" />
<script src="_content/TabBlazor/js/tabblazor.js"></script>
```

`YourApp.styles.css` is the scoped CSS bundle that the Blazor project templates already link. Components such as Tabs, DataView and Dropdown take part of their styling from it.

This package includes `AGENTS.md`, usage notes for AI coding agents.

## Quick example

```razor
<Card>
    <CardHeader>
        <CardTitle>Hello, TabBlazor</CardTitle>
    </CardHeader>
    <CardBody>
        <Button BackgroundColor="TablerColor.Primary" OnClick="Save">Save</Button>
    </CardBody>
</Card>
```

```razor
<DataView Items="orders" Title="Orders" KeySelector="o => o.Id" PageSize="10">
    <DataColumn Value="o => o.OrderNo" Title="Order" Sortable Searchable />
    <DataColumn Value="o => o.OrderDate" Title="Date" Format="yyyy-MM-dd" Sortable DefaultSort="SortOrder.Descending" />
    <DataColumn Value="o => o.Total" Format="N2" Sortable />
</DataView>
```

## Related packages

- `TabBlazor.QuickTable.EntityFramework` — EF Core async query provider for QuickTable

## Templates

Project templates: [TabBlazor.Templates](https://github.com/TabBlazor/TabBlazor.Templates).

## License

MIT
