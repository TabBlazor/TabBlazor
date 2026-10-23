# TabBlazor — AI agent guide

Usage notes for AI coding agents consuming the `TabBlazor` NuGet package. Goal: wire up
the library, pick the right component and use it correctly without reading the source.

**Parameter reference:** `componentapi.json` (package root) is a machine-readable manifest
of every component and its parameters — name, type, whether it's
`[Parameter]`/`[CascadingParameter]`, `EditorRequired`, default value, the declaring type
for inherited params, and the XML summary. It is generated from the built assembly on every
pack, so it matches this version. Schema: `tabblazor-component-api/v1`. For non-component
types (services, options, `DataRequest`, …) read the XML docs (`TabBlazor.xml`, next to
`TabBlazor.dll`) or step into source via Source Link.

This file covers what the manifest can't: which component to use, how the pieces fit
together, and the traps.

- Packages: `TabBlazor` (components), `TabBlazor.QuickTable.EntityFramework` (optional EF Core adapter for `QuickTable` only)
- Root namespace: `TabBlazor`
- Target: .NET 10, Blazor Server and WebAssembly
- UI base: [Tabler](https://preview.tabler.io/) 1.7 (Bootstrap-based). Minimal JS interop.
- Live examples with source: https://tabblazor.com

## Setup (required)

1. Register services in `Program.cs`:
   ```csharp
   builder.Services.AddTabBlazor(options =>
   {
       options.EnablePopper = true; // optional: dynamic tooltip/dropdown/popover positioning
   });
   ```
   `AddTabBlazor` returns a `TabBlazorBuilder`. Chain `.AddValidation<TValidator>()` to plug
   in a custom `IFormValidator` (default is DataAnnotations). See [Options](#options).

2. Add to `_Imports.razor`:
   ```razor
   @using TabBlazor
   ```

3. Reference styles and script in `App.razor` (Server) or `index.html` (WebAssembly):
   ```html
   <link rel="stylesheet" href="https://unpkg.com/@tabler/core@1.7.0/dist/css/tabler.min.css" />
   <link rel="stylesheet" href="_content/TabBlazor/css/tabblazor.min.css" />
   <link rel="stylesheet" href="YourApp.styles.css" />
   <script src="_content/TabBlazor/js/tabblazor.js"></script>
   ```
   `YourApp.styles.css` is the app's scoped-CSS bundle (the Blazor templates link it already;
   in .NET 9+ templates it is `@Assets["YourApp.styles.css"]`). It imports TabBlazor's scoped
   styles; without it Tabs, DataView, Dropdown, Datepicker, Offcanvas, Tooltip, Popover and
   RangeSlider render without part of their styling.

4. Put the hosts for the services once in the main layout. Without them
   `IModalService`, `IOffcanvasService` and `ToastService` calls show nothing:
   ```razor
   <ModalContainer />
   <ToastContainer />
   <OffcanvasContainer />
   ```

## Pick the right component

| Need | Use |
| --- | --- |
| List or table of records, new code | `DataView` (preview) — see below |
| Existing screens built on `Table` / `QuickTable` | Keep them; they are unchanged. Don't migrate unless asked. |
| Form with validation | `TablerForm` + inputs |
| Tabbed page or card | `Tabs` / `Tab` |
| Dialog returning a result | `IModalService.ShowAsync<T>` |
| Yes/no confirm | `IModalService.ShowDialogAsync` |
| Side panel | `<Offcanvas>` (declarative) or `IOffcanvasService` |
| Notification | `ToastService` |
| Admin layout | `Page`, `Navbar`, `Card` — see the demo site's layout |

## Component conventions

- Most components inherit `TablerBaseComponent`. Common params: `TextColor`,
  `BackgroundColor` (both `TablerColor`), `ChildContent`, `OnClick`.
- Unmatched HTML attributes pass through to the root element. A user-supplied `class` is
  merged with the component's classes, not replaced.
- Colors come from the `TablerColor` enum (`TablerColor.Primary`, `TablerColor.Danger`, …).
  Don't hand-write Bootstrap color classes on TabBlazor components.

## Icons: no icon set ships

Components that take an icon (`Icon`, `Tab.Icon`, `RowAction.Icon`, …) want an `IIconType`.
The package contains **no icon catalog**. `TablerIcons.Heart` and friends in the demo site
come from a static class in the docs project, not from the package. Create your own with the
icons you use; the SVG elements come from https://tabler.io/icons:

```csharp
public static class AppIcons
{
    public static IIconType Heart => new TablerIcon(
        "<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M19.5 12.572l-7.5 7.428l-7.5 -7.428a5 5 0 1 1 7.5 -6.566a5 5 0 1 1 7.5 6.572' />");
}
```
```razor
<Icon IconType="AppIcons.Heart" Size="16" TextColor="TablerColor.Red" />
```
`MDIcon` does the same for Material Design icon paths.

## DataView (preview)

One component for tables and lists: paging, sorting, word search, column filters, editing,
selection, row and bulk actions, detail rows, virtualization and URL state.

### Data in — pick one

- `Items` — an in-memory `IEnumerable<TItem>`. Paging, sorting, search and filters run in memory.
- `ItemsProvider` — `Func<DataRequest, CancellationToken, Task<DataPage<TItem>>>` for a server or
  database. Translate the request yourself and pass the token on.
- `Source` — an `IDataViewSource<TItem>` (same contract as a class; `InMemorySource<TItem>` is
  the built-in one).

`DataRequest` carries `Page` (offset or cursor), `Sorts`, `Search`, `Filters` and `Columns`.
Sorts and filters name columns by id. Return `new DataPage<TItem>(items, totalCount)`, or
`(items, null, nextCursor)` with `PagingMode.Cursor`.

Always set `KeySelector` (`o => o.Id`): selection, editing and stable paging rely on it.

```razor
<DataView Items="orders" Title="Orders" KeySelector="o => o.Id" PageSize="10">
    <DataColumn Value="o => o.OrderNo" Title="Order" Sortable Searchable />
    <DataColumn Value="o => o.Customer.Name" Title="Customer" Sortable Searchable />
    <DataColumn Value="o => o.Status" Sortable Filterable />
    <DataColumn Value="o => o.OrderDate" Format="yyyy-MM-dd" Sortable DefaultSort="SortOrder.Descending" />
    <DataColumn Value="o => o.Total" Format="N2" Align="Align.End" Sortable />
</DataView>
```

```razor
<DataView TItem="Order" ItemsProvider="LoadAsync" KeySelector="o => o.Id" Title="Orders">
    ...
</DataView>

@code {
    private async Task<DataPage<Order>> LoadAsync(DataRequest request, CancellationToken cancellationToken)
    {
        // EF Core: create one DbContext per call from IDbContextFactory<T>, apply request.Sorts,
        // request.Search and request.Filters, then Skip/Take from request.Page.
        return await api.GetOrdersAsync(request, cancellationToken);
    }
}
```

### Columns

- `DataColumn Value="o => …"` takes a member path or a calculated value. Use `Template`
  (`RenderFragment<TItem>`) for custom cell content.
- Column id: the camelCased member path (`customer.name`); for a calculated value, the members
  it uses joined with `-` (`firstName-lastName`). Override with `Id`, `SortBy`, `FilterBy`.
  These ids appear in `DataRequest`, URL state and `EditResult.Invalid` field names.
- Useful flags: `Sortable`, `Searchable`, `Filterable`, `DefaultSort`, `Format`, `Align`,
  `HideBelow` (breakpoint), `Visible`, `Editable`/`ReadOnly`, `Options` (choices for filters and editors).

### Editing

```razor
<DataView Items="orders" KeySelector="o => o.Id" EditMode="DataEditMode.Inline"
          SaveItem="SaveAsync" DeleteItem="DeleteAsync" NewItem="() => new Order()">
    ...
</DataView>

@code {
    // ctx.Draft is a copy; the row only changes when this returns Saved.
    private async Task<EditResult<Order>> SaveAsync(DataEditContext<Order> ctx)
    {
        if (await api.OrderNoTakenAsync(ctx.Draft))
            return EditResult.Invalid(("orderNo", "Order number already exists"));
        return EditResult.Saved(await api.SaveAsync(ctx.Draft));
    }

    private async Task<EditResult> DeleteAsync(Order order)
    {
        await api.DeleteAsync(order.Id);
        return EditResult.Deleted();
    }
}
```
`EditMode`: `Inline`, `Popup`, `Drawer`, or `Custom` (handle `OnCustomEdit` yourself).
Failures: `EditResult.Failed("message")`.

### Actions, selection, details, views

```razor
<DataView Items="orders" KeySelector="o => o.Id"
          SelectionMode="DataSelectionMode.Multiple" @bind-SelectedItems="selected"
          DeleteItems="DeleteSelectedAsync" DetailsMode="DataDetailsMode.Drawer"
          Views="DataViewKind.Table | DataViewKind.List">
    <DataColumn Value="o => o.OrderNo" Sortable Searchable />
    <RowActions>
        <EditAction />
        <DeleteAction />
        <RowAction Title="Open" Icon="AppIcons.External" Href="@(o => $"orders/{o.Id}")" />
        <RowAction Title="Archive" OnClick="@(o => ArchiveAsync(o))" Confirm="Archive this order?" Danger />
    </RowActions>
    <BulkActions>
        <BulkAction Title="Export" OnClick="@(s => ExportAsync(s))" />
        <BulkDeleteAction />
    </BulkActions>
    <DetailsTemplate>@context.Item.Notes</DetailsTemplate>
    <ListItemTemplate>@context.OrderNo</ListItemTemplate>
</DataView>
```
- `SelectedItems` is an `IReadOnlyList<TItem>` that survives paging and search. Bulk handlers get
  a `DataSelection<TItem>`; when `AllMatching` is true it carries the request instead of every row.
- Other slots: `ToolbarTemplate`, `HeaderTemplate`, `EmptyTemplate`, `NoResultsTemplate`,
  `LoadingTemplate`, `ErrorTemplate` (gets the `Exception`), `MobileTemplate`, `RowActionsTemplate`.
- `UrlState` (with a unique `Id`) keeps page, sort, search and filters in the query string;
  `UrlHistory="UrlHistoryMode.Push"` adds a history entry per change.
- `Virtualize` shows one scrolling list instead of pages. It needs a fixed row height
  (`ItemSize`) and a `Height` on the view.
- UI strings: the `Text` parameter, or app-wide via [Options](#options).

## Tabs

```razor
<Tabs UrlParameter="tab">
    <Tab Id="overview" Title="Overview" Icon="AppIcons.Dashboard">…</Tab>
    <Tab Id="orders" Title="Orders" Badge="@orderCount.ToString()" OnActivated="LoadOrdersAsync">…</Tab>
</Tabs>
```
- `UrlParameter` makes `?tab=orders` select a tab (by `Id`, else 1-based position). Each Tabs on a
  page needs its own parameter name.
- Inactive tabs are not rendered until visited. `KeepAlive` keeps visited tabs rendered;
  `Preload` renders them ahead (`TabPreload.Hover` or `TabPreload.Eager`).
- Phones get a sideways-scrolling tab row. `MobilePlacement="TabsMobilePlacement.Bottom"` pins
  the tabs to a bottom bar instead — use it for **one page-level Tabs only**, with an `Icon` on
  every tab. `ShowMobileTitle` controls the title strip above the content.

## Forms

```razor
<TablerForm Model="model" OnValidSubmit="SaveAsync">
    <div class="mb-3">
        <label class="form-label">Name</label>
        <InputText class="form-control" @bind-Value="model.Name" />
        <TabValidationMessage For="() => model.Name" />
    </div>
    <div class="mb-3">
        <Select Items="countries" @bind-SelectedValue="model.CountryCode"
                TextExpression="c => c.Name" ConvertExpression="c => c.Code" />
    </div>
    <Button Type="ButtonType.Submit" BackgroundColor="TablerColor.Primary">Save</Button>
</TablerForm>
```
- Text fields use Blazor's own `InputText`/`InputNumber`/`InputTextArea` with Bootstrap classes.
  There is no `TextInput` component.
- TabBlazor inputs: `Select`, `ItemSelect` (single or multi), `Typeahead`, `Autocomplete`,
  `Datepicker`, `NumberInput`, `ValueInput`, `Checkbox`, `RangeSlider`. Binding names differ per
  input (`@bind-Value`, `@bind-SelectedValue`, `@bind-SelectedValues`, `@bind-SelectedDate`) —
  check the manifest. `Select`/`ItemSelect` need `ConvertExpression` when the bound value is not
  the item itself.

## Modals

```csharp
@inject IModalService ModalService

var result = await ModalService.ShowAsync("Edit order",
    new RenderComponent<EditOrder>().Set(c => c.OrderId, id));

if (!result.Cancelled)
{
    var data = result.Data; // from ModalService.Close(ModalResult.Ok(data))
}
```
- Inside the modal component, inject `IModalService` and call `Close(ModalResult.Ok(payload))`,
  or `Close()` to cancel.
- Confirm: `await ModalService.ShowDialogAsync(new DialogOptions { … })` returns `bool`.

## Offcanvas

- Declarative: `<Offcanvas @bind-Open="open" Title="Details" Position="OffcanvasPosition.End" Size="OffcanvasSize.Medium" OnClosing="…">…</Offcanvas>`.
- Service: `await OffcanvasService.ShowAsync("Title", new RenderComponent<T>().Set(…))` returns an
  `OffcanvasResult`; close from inside with `IOffcanvasService.Close(…)`.

## Toasts

```csharp
await ToastService.AddToastAsync(new ToastModel
{
    Title = "Saved",
    Message = "The order was saved.",
    Options = new ToastOptions { Delay = 0 } // 0 = stays until closed
});
```

## Options

Set in `AddTabBlazor(options => …)`:

- `EnablePopper` — load Popper (from `PopperScriptUrl`) for positioning. Also turned on by a
  non-default `DefaultPositioning`.
- `DefaultPositioning` — default for every component with a `Positioning` parameter.
- `Tabs` — app-wide `Preload`, `PreloadDelay`, `KeepAlive`, `UrlHistory` for Tabs.
- `DataViewText` — app-wide DataView UI strings.
- `DataViewTextResource` — a resource marker type to translate DataView strings through
  `IStringLocalizerFactory` (`services.AddLocalization()`); keys are the `DataViewText` property names.

## Registered services (from AddTabBlazor)

`IModalService`, `IOffcanvasService`, `ToastService`, `TablerService` (JS interop),
`IFormValidator` (default `TablerDataAnnotationsValidator`), `FlagService`,
`TableFilterService`, plus internal registries for Tabs/DataView URL state and overlay
stacking. `IPopperService` is registered only when Popper is enabled.

## Traps

- **No icons in the package** — see [Icons](#icons-no-icon-set-ships).
- **Missing layout hosts** — modal/offcanvas/toast service calls do nothing without
  `ModalContainer`, `OffcanvasContainer`, `ToastContainer`.
- **Missing `YourApp.styles.css`** — components look partly unstyled.
- **`Align` is ambiguous** when another library also has one (e.g. ApexCharts). Add
  `@using Align = TabBlazor.Align`.
- **bUnit tests:** TabBlazor has a `RenderComponent<T>` type. Use bUnit's `Render<T>(…)`.
- **Tabler CSS version:** link the Tabler version this package targets (1.7.x), not an older one.
- **`TabBlazor.QuickTable.EntityFramework` is for QuickTable only.** DataView needs no
  adapter: query EF Core in an `ItemsProvider`.

## Where to look for more

- `componentapi.json` and `TabBlazor.xml` in the package; Source Link for exact source.
- https://tabblazor.com — every component with live examples and their source.
- Component folders in the repo (`src/TabBlazor/Components/`): Accordions, Alerts, Avatars,
  Badges, Buttons, Cards, Carousel, Confetti, Dashboards, DataGrid, DataViews, Dimmers,
  Dropdowns, Flags, Forms, Icons, Inputs, Layouts, Modals, Navbars, Navigation, ObjectBrowser,
  Offcanvas, Pages, Popovers, Progresses, QuickTables, RangeSliders, Signals, Sparklines,
  Statuses, SwitchContent, Tables, Tabs, Timelines, Toasts, Tooltips, TreeViews, Utilities.
