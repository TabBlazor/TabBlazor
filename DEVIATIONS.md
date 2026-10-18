# DataView: deviations from the design doc

Running list of places where the implementation differs from, or fills a gap in, the
"DataView design" doc. Use it to update the doc.

## Phase 1: core and table view

- **`PageRequest` is one record**, `PageRequest(Mode, Skip, Take, After)` with
  `PageRequest.Offset(skip, take)` and `PageRequest.Cursor(after, first)` factories, instead
  of two shapes. One record serializes to JSON without polymorphism. `First` is an alias of `Take`.
- **`DataColumnInfo` carries more than the doc lists.** `ValueType` is `[JsonIgnore]`
  (System.Text.Json refuses to serialize `System.Type`), so a serialized `Kind`
  (`DataValueKind`: Text, Number, Boolean, Date, Enum, Guid, Other) tells servers how to treat
  search terms and filter values. For in-process sources it also carries `[JsonIgnore]`
  `FilterValueType`, `SortExpressions`, `FilterExpressions` (the `SortBy`/`FilterBy` parts, or
  `Value`) and `SearchExpression`. These are additive init properties; the positional record is
  as in the doc.
- **The `DataColumnInfo.FilterMode` enum is named `FilterPathMode`** (Exact, Parts), so the name
  `FilterMode` stays free for the view's planned `FilterMode="Row"` parameter.
- **Calculated values without overrides:** a calculated *text* value gets its members as Parts
  paths; a calculated *non-text* value (`o => o.Quantity * o.Price`) gets an Id
  (`quantity-price`) but empty `SortPaths`/`FilterPaths`, which is how a remote source knows to
  report "map this column" at load. In memory it sorts and searches on the value itself.
- **Member paths stop at simple values and collections:** `o => o.OrderDate.Year` uses
  `orderDate`, `o => o.Lines.Count` uses `lines`. Such expressions count as calculated.
- **A `DataColumn` without a usable `Value`** (none, or one that reads no members) needs an `Id`;
  otherwise it falls back to its `Title` in camelCase, then `columnN`.
- **Key as the last sort:** when the user has sorted, the key is always appended. When nothing is
  sorted, the key is sent to `Source`/`ItemsProvider` but not used for `Items`, so a local list
  keeps its own order.
- **Read-only state is `CurrentState`**, not `State`: `State` is the parameter behind
  `@bind-State`. The doc's `OnStateChanged` event is `StateChanged` (needed for `@bind-State`);
  there is no separate `OnStateChanged`.
- **`SearchDelay` is an `int` in milliseconds** (default 300), and `TimeProvider` is resolved from
  DI when registered, so tests can control the delay.
- **First load:** `Items` loads synchronously during the first render (so prerendering and the
  first paint have rows); `Source` and `ItemsProvider` load after the first interactive render.
- **Row clicks:** without JS, the view cannot tell which element was clicked. Our own controls
  stop propagation; a cell `Template` that handles its own click should add
  `@onclick:stopPropagation` to keep `OnRowClicked` from firing. With `RowHref`, links, buttons
  and inputs inside cells sit above the row link via CSS; other elements can add the
  `dv-interactive` class.
- **Localization:** strings live in a `DataViewText` record with English defaults, overridable
  per view with `Text`. `IStringLocalizer` integration is left for phase 7.
- **The density menu ("⋮")** appears only when `Density` is not set by the page.
- **The in-memory source already evaluates `FilterSpec`s** (all operators, Exact and Parts), so the
  contract is complete; the filter UI comes in phase 5.
- **The shared source test kit** (`DataViewSourceTests<TSource>`) lives in `TabBlazor.Tests`
  with a fixed `SourceTestItem` type and is not shipped in a package yet.
- **Errors at load** (two columns with the same Id) are thrown as `InvalidOperationException`
  while rendering.
- `Align` (TabBlazor) clashes with `ApexCharts.Align` in apps that import both; the docs examples
  use `@using Align = TabBlazor.Align`, as the Table docs already do.

## Phase 2: list view

- **Templates are slot components, not `RenderFragment` parameters.** Razor does not allow
  columns and named templates side by side (`<DataColumn …/>` next to `<ListItemTemplate>`)
  unless the columns are wrapped in `<ChildContent>`. So that the doc's markup works as
  written, `HeaderTemplate`, `ToolbarTemplate`, `LoadingTemplate`, `EmptyTemplate`,
  `NoResultsTemplate`, `ErrorTemplate` (context: the exception) and `ListItemTemplate` (context:
  the item) are small components that register with the view, in any order among the columns.
  Inside other components with a parameter of the same name (Table's `HeaderTemplate`), the
  parameter still wins, so existing markup is unaffected. Later templates (`DetailsTemplate`,
  `RowActions`, …) follow the same pattern.
- **The view enum is `DataViewKind`, not `DataViews`.** A type named `DataViews` clashes with
  any namespace ending in `.DataViews` (a common folder name; our own docs and tests hit it).
  Usage: `View="DataViewKind.List" Views="DataViewKind.Table | DataViewKind.List"`.
- **`View` is nullable** (`DataViewKind?`, with `ViewChanged` for `@bind-View`); when not set,
  the first view in `Views` is shown (table when it is included).
- **`DataListItem`** is an extra helper component for the doc's list layout (avatar, title,
  subtitle, meta on the right), for use inside `ListItemTemplate`.
- In the list view, `RowColor` maps every color to Tabler's light backgrounds (`bg-danger-lt`),
  since the table's contextual classes (`table-danger`) do not apply to list items.
- The view switcher sits after `ToolbarTemplate`, followed by the sort menu (list view only),
  then search; the doc did not place it.
