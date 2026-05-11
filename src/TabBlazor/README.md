# TabBlazor

Blazor component library built on the [Tabler](https://tabler.io/) UI framework.

- 90+ Razor components: forms, tables, modals, dashboards, navigation
- **QuickTable** with virtual scrolling, sort, filter, group
- Pluggable form validation (DataAnnotations by default)
- Modal and offcanvas services with async result patterns
- Dark/light theme switch
- Minimal JavaScript interop

**[Live demo & docs](https://tabblazor.com)** · **[GitHub](https://github.com/TabBlazor/TabBlazor)**

## Install

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

## Related packages

- `TabBlazor.QuickTable.EntityFramework` — EF Core async query provider for QuickTable

## Templates

Project templates: [TabBlazor.Templates](https://github.com/TabBlazor/TabBlazor.Templates).

## License

MIT
