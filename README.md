# TabBlazor

A Blazor component library and admin template built on the [Tabler](https://preview.tabler.io/) UI framework.

[![Build](https://github.com/TabBlazor/TabBlazor/actions/workflows/ci.yml/badge.svg)](https://github.com/TabBlazor/TabBlazor/actions/workflows/ci.yml?branch=master)
[![NuGet](https://img.shields.io/nuget/v/TabBlazor.svg?label=TabBlazor)](https://www.nuget.org/packages/TabBlazor/)
[![NuGet](https://img.shields.io/nuget/v/TabBlazor.QuickTable.EntityFramework.svg?label=QuickTable.EntityFramework)](https://www.nuget.org/packages/TabBlazor.QuickTable.EntityFramework/)
[![Downloads](https://img.shields.io/nuget/dt/TabBlazor.svg)](https://www.nuget.org/packages/TabBlazor/)

**[Live demo](https://tabblazor.com)**

![Dashboard](TabBlazorDashbord.png?raw=true "Dashboard")

## Features

- 90+ Razor components on Tabler 1.7: forms, cards, modals, offcanvas, navigation, dashboards
- **DataView** (preview): one data component for tables and lists, with sorting, search, column filters, editing, selection, row actions, detail rows, virtualization and URL state. It reads from any `IDataViewSource<T>`, such as an in-memory list or your own EF Core or API query.
- **QuickTable** and **Table** for existing screens; optional EF Core adapter for QuickTable
- **Tabs** with deep links, preloading, keep-alive and a bottom tab bar on phones
- Form inputs (Autocomplete, Typeahead, ItemSelect, Datepicker, NumberInput, Select, range slider) with pluggable validation: DataAnnotations by default, or bring your own `IFormValidator`
- Modal, offcanvas and toast services with async results
- Sparkline, Signal, Status, Timeline, TreeView, Confetti and more
- Dark/light theme switch; localizable DataView texts
- Minimal JavaScript interop; opt-in Popper positioning for tooltips and dropdowns

## Install

Requires .NET 10.

```bash
dotnet add package TabBlazor
dotnet add package TabBlazor.QuickTable.EntityFramework   # optional, EF Core adapter for QuickTable
```

Register services in `Program.cs`:

```csharp
builder.Services.AddTabBlazor(options =>
{
    options.EnablePopper = true;   // optional, enables dynamic tooltip/dropdown positioning
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

The package also ships an `AGENTS.md` with usage notes for AI coding agents.

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

A table with sorting, search and paging:

```razor
<DataView Items="orders" Title="Orders" KeySelector="o => o.Id" PageSize="10">
    <DataColumn Value="o => o.OrderNo" Title="Order" Sortable Searchable />
    <DataColumn Value="o => o.Customer" Sortable Searchable />
    <DataColumn Value="o => o.OrderDate" Title="Date" Format="yyyy-MM-dd" Sortable DefaultSort="SortOrder.Descending" />
    <DataColumn Value="o => o.Total" Format="N2" Sortable />
</DataView>
```

See the [demo site](https://tabblazor.com) for hundreds of working examples with source.

## Templates

Project templates for getting started fast: [TabBlazor.Templates](https://github.com/TabBlazor/TabBlazor.Templates).

## Contributing

Issues and pull requests welcome. To run the demo locally:

```bash
cd docs/Tabler.Docs.Wasm
dotnet run
```

Build and test the full solution:

```bash
dotnet build TabBlazor.slnx -c Release
dotnet test
```

## License

MIT, see [LICENSE](LICENSE).

## Credits

Originally based on [Tabler.Blazor](https://github.com/zarxor/Tabler.Blazor) by [zarxor](https://github.com/zarxor).
UI framework: [Tabler](https://tabler.io/).
