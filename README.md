# TabBlazor

A Blazor component library and admin template built on the [Tabler](https://preview.tabler.io/) UI framework.

[![Build](https://github.com/TabBlazor/TabBlazor/actions/workflows/ci.yml/badge.svg)](https://github.com/TabBlazor/TabBlazor/actions/workflows/ci.yml?branch=master)
[![NuGet](https://img.shields.io/nuget/v/TabBlazor.svg?label=TabBlazor)](https://www.nuget.org/packages/TabBlazor/)
[![NuGet](https://img.shields.io/nuget/v/TabBlazor.QuickTable.EntityFramework.svg?label=QuickTable.EntityFramework)](https://www.nuget.org/packages/TabBlazor.QuickTable.EntityFramework/)
[![Downloads](https://img.shields.io/nuget/dt/TabBlazor.svg)](https://www.nuget.org/packages/TabBlazor/)

**[Live demo](https://tabblazor.com)**

![Dashboard](TabBlazorDashbord.png?raw=true "Dashboard")

## Features

- 90+ Razor components (forms, tables, modals, dashboards, navigation)
- **QuickTable** with virtual scrolling, sorting, filtering, grouping; optional EF Core integration
- Pluggable form validation (DataAnnotations by default; bring your own `IFormValidator`)
- Modal and offcanvas services with async result patterns
- Dark/light theme switch
- Minimal JavaScript interop — most components are pure C#/CSS
- Opt-in popper.js positioning for tooltips, dropdowns, etc.

## Install

```bash
dotnet add package TabBlazor
dotnet add package TabBlazor.QuickTable.EntityFramework   # optional, EF Core integration
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
<link rel="stylesheet" href="https://unpkg.com/@tabler/core@1.4.0/dist/css/tabler.min.css" />
<link rel="stylesheet" href="_content/TabBlazor/css/tabblazor.min.css" />
<script src="_content/TabBlazor/js/tabblazor.js"></script>
```

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

## Credits

Originally based on [Tabler.Blazor](https://github.com/zarxor/Tabler.Blazor) by [zarxor](https://github.com/zarxor).
UI framework: [Tabler](https://tabler.io/).
