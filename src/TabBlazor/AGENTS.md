# TabBlazor — AI agent guide

Machine-readable usage notes for AI coding agents consuming the `TabBlazor` NuGet
package. Goal: enough to wire up the library and use the main components correctly
without reading the source. For per-symbol detail, read the shipped XML docs
(`TabBlazor.xml`, next to `TabBlazor.dll` in the package) or step into source via
Source Link.

- Package: `TabBlazor` (component library), `TabBlazor.QuickTable.EntityFramework` (optional EF Core integration)
- Root namespace: `TabBlazor`
- Target: .NET 10, Blazor (Server + WebAssembly). `browser` is a supported platform.
- UI base: [Tabler](https://preview.tabler.io/) (Bootstrap-based). Minimal JS interop.
- Live examples with source: https://tabblazor.com

## Setup (required)

1. Register services in `Program.cs`:
   ```csharp
   builder.Services.AddTabBlazor(options =>
   {
       options.EnablePopper = true; // enables dynamic tooltip/dropdown/popover positioning
   });
   ```
   `AddTabBlazor` returns a `TabBlazorBuilder`. Chain `.AddValidation<TValidator>()`
   to plug a custom `IFormValidator` (default is DataAnnotations).

2. Add to `_Imports.razor`:
   ```razor
   @using TabBlazor
   ```

3. Reference styles + script in `App.razor` (Server) or `index.html` (WebAssembly):
   ```html
   <link rel="stylesheet" href="https://unpkg.com/@tabler/core@1.4.0/dist/css/tabler.min.css" />
   <link rel="stylesheet" href="_content/TabBlazor/css/tabblazor.min.css" />
   <script src="_content/TabBlazor/js/tabblazor.js"></script>
   ```

## Component conventions

- All components inherit `TablerBaseComponent`. Common params: `TextColor`,
  `BackgroundColor` (both `TablerColor` enum), `ChildContent`, `OnClick`.
- Unmatched HTML attributes pass through to the root element (`CaptureUnmatchedValues`).
  A user-supplied `class` is merged with component classes, not replaced.
- Colors come from the `TablerColor` enum (e.g. `TablerColor.Primary`,
  `TablerColor.Danger`). Do not hand-write Bootstrap color class strings.

## Common tasks

### Card + button
```razor
<Card>
    <CardHeader><CardTitle>Title</CardTitle></CardHeader>
    <CardBody>
        <Button BackgroundColor="TablerColor.Primary" OnClick="Save">Save</Button>
    </CardBody>
</Card>
```

### Show a modal
Inject `IModalService`, then:
```csharp
@inject IModalService ModalService

var result = await ModalService.ShowAsync<MyComponent>(
    "Title",
    new RenderComponent<MyComponent>().Set(c => c.SomeParam, value));

if (!result.Cancelled)
{
    var data = result.Data; // payload passed to ModalService.Close(ModalResult.Ok(data))
}
```
- Confirm dialogs: `await ModalService.ShowDialogAsync(new DialogOptions { ... })` returns `bool`.
- Close from inside a modal component: `ModalService.Close(ModalResult.Ok(payload))` or `ModalService.Close()` to cancel.

### Forms
Use `<TablerForm Model="model" OnValidSubmit="...">` with built-in inputs
(`TextInput`, `Select`, `Autocomplete`, `Typeahead`, `Datepicker`, etc.).
Validation is pluggable via `IFormValidator`; DataAnnotations is the default.

### Tables
- `QuickTable<T>` — virtual-scrolling grid. Supply rows via `GridItemsProvider<T>`.
  Columns: `PropertyColumn`, `TemplateColumn`. Supports sort/filter/group.
  Server-side EF Core querying lives in `TabBlazor.QuickTable.EntityFramework`.
- `Table<T>` — standard table with editable rows, detail rows, grouping, column config.

### Toasts / Offcanvas
Inject `ToastService` / `IOffcanvasService` (registered by `AddTabBlazor`).

## Registered services (from AddTabBlazor)

`ToastService`, `TablerService` (JS interop), `IOffcanvasService`, `IModalService`,
`TableFilterService`, `IFormValidator` (default `TablerDataAnnotationsValidator`),
`FlagService`. `IPopperService` is registered only when `EnablePopper` is true (or a
non-default `DefaultPositioning` is set).

## Where to look for more

- XML doc comments ship in the package (`TabBlazor.xml`) — primary per-symbol reference.
- Source Link is enabled: debuggers/agents can fetch exact source for any symbol.
- Component categories live under `Components/` in the repo: Accordions, Alerts,
  Avatars, Badges, Buttons, Cards, Carousel, Dashboards, DataGrid, Dimmers, Dropdowns,
  Flags, Forms, Icons, Inputs, Layouts, Modals, Navbars, Navigation, Offcanvas, Pages,
  Popovers, Progresses, QuickTables, RangeSliders, Statuses, Tables, Tabs, Timelines,
  Toasts, Tooltips, TreeViews, Utilities.
