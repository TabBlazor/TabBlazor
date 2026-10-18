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

## Phase 3: Offcanvas additions

- **Already on master:** unique title ids, moving focus into the panel, and unsubscribing
  `OffcanvasContainer` from `OnChanged` (the leak) were done before this work. Added here: focus
  goes back to the element that had it when the panel closes (`tabBlazor.saveFocus` /
  `restoreFocus`, exposed as `TablerService.SaveFocusAsync` / `RestoreFocusAsync`).
- **`OnClosing`** is an `EventCallback<OffcanvasClosingEventArgs>` (set `Cancel = true` to stay
  open) on `OffcanvasView` and the declarative `Offcanvas`, plus an `OffcanvasOptions.OnClosing`
  delegate for panels opened through `IOffcanvasService`.
- **`Size`** is `OffcanvasSize` (Default, Small, Medium, Large, Full), applied as Tabler's
  `--tblr-offcanvas-width`/`-height` variables, on `OffcanvasOptions` and the declarative component.
- **Animation:** opening is a CSS keyframe animation (the panel still renders with `show` at once,
  so nothing changes for the service or for prerendering). Closing animates only for the
  declarative component (it keeps the panel 250 ms with `offcanvas-hiding`); panels opened by the
  service are still removed at once. Both honour `prefers-reduced-motion`.
- **Declarative `Offcanvas`:** `@bind-Open`, `Title`/`TitleTemplate`, `FooterTemplate`,
  `Position` (default End), `Size`, `Backdrop`, `CloseOnEsc` (default true here), 
  `CloseOnClickOutside`, `Floating`, `Class`, `OnClosing`, `OnClosed`, and `CloseAsync()`. As a
  general-purpose component it uses ordinary parameters, so body content goes in `<ChildContent>`
  when `<FooterTemplate>` is used.
- `OffcanvasView` sets `aria-modal` only when it has a backdrop, renders an optional
  `.offcanvas-footer`, and now also implements `IAsyncDisposable` (to give focus back).
- The docs page `Offcanvas.razor` was renamed `OffcanvasDocs.razor` (same route), because a page
  component named `Offcanvas` would hide the new `<Offcanvas>` tag in that folder.

## Phase 3: editing, selection, row actions and details

- **Selection** is turned on with `SelectionMode` (None, Single, Multiple); the doc left the switch
  unnamed. `GetSelection()` returns a `DataSelection<TItem>` (Items, Keys, AllMatching, Request,
  TotalCount). The doc's `ClearSelection()` is `ClearSelectionAsync()`. With "select all
  matching", the selection's `Request` is the current request with `Take` = the total.
- **Delete handlers** return `Task<EditResult>` (`EditResult.Deleted()` or `Failed(...)`);
  `DeleteItems` receives the `DataSelection<TItem>`. `EditResult.Deleted()` is an addition.
- **`EditResult`** is a non-generic factory class whose `Failed`/`Invalid` results convert
  implicitly to `EditResult<TItem>`, so a save handler can `return EditResult.Failed(...)`.
- **Custom edit mode** uses an `OnCustomEdit` callback that receives the `DataEditContext`; your
  UI calls its `SaveAsync()` (normal validation and save flow) or `CancelAsync()`.
- **Confirmations** (delete, discard changes, action `Confirm`) use `IModalService.ShowDialogAsync`
  by default; an `OnConfirm` parameter replaces it (used by tests and apps with their own dialog).
- **Errors** from row actions, bulk actions and failed deletes show in a dismissible notice under
  the header. Failed saves show on the edit row or form, as in the doc.
- **Default editors** are Blazor's `InputText`, `InputNumber`, `InputDate`, `InputCheckbox` and
  `InputSelect` (enums, `bool?`, `Options`), so validation styling and modified state work.
  Other types (Guid, …) need an `EditorTemplate`. Data annotations are enabled on the draft's
  `EditContext` directly; a custom `IFormValidator`'s component is rendered inside the edit UI.
- **Popup and drawer forms** list `ShowIn != ViewOnly` columns (hidden ones too); fields without
  an editor show as plain text, and are left out when adding. The popup is TabBlazor's
  `ModalView` rendered by the view (full screen below `md`); the drawer is the new declarative
  `Offcanvas` (Large). The list view uses the popup when `EditMode` is Inline.
- **Saving with local `Items`:** when `Items` is a writable `IList`, the saved item replaces the
  original in the list (or is appended when new), and deletes remove it; arrays and read-only
  lists are left alone. With a remote source, an edited row is replaced by key without
  reloading; a new item reloads the page and shows "Saved…" when it is filtered out.
- **Actions are components that register while alive.** Actions without delegate parameters
  (`<EditAction />`) are not re-rendered by Blazor, so they register on initialization and keep
  their place; markup order comes from the latest render pass.
- **Generic inference:** Razor cannot infer `TItem` for `RowAction`/`BulkAction` from a method
  group (`OnClick="ArchiveAsync"`), so use a lambda (`OnClick="@(o => ArchiveAsync(o))"`) or set
  `TItem`. `<EditAction />`, `<DeleteAction />` and lambdas infer it from the view.
- **`RowClick`** is a `DataRowClick` enum with an extra `Auto` default: navigate when `RowHref` is
  set, else open details when there is a `DetailsTemplate`, else select in single mode.
- **`LoadDetails`** is a `Func<TItem, Task>` (any `Task<T>` fits); the template reads the result
  with `context.Get<TDetails>()`, because `DataView<TItem>` cannot know `TDetails`. A
  `DetailsTitle` parameter names the drawer and side panel. The side panel works in both views.
- **`DataRowContext`** methods use the doc's names (`Edit()`, `Save()`, `Cancel()`, `Delete()`,
  `ToggleDetails()`, plus `ToggleSelected()`) and return tasks.
- **Not yet:** arrow-key navigation in row action menus and rows (Tab between editors works);
  the header's select-all checkbox has a tooltip but no `aria-label`, because TabBlazor's
  `CheckboxTriState` takes no extra attributes.

## Phase 4: virtualization, QuickTable parity and small screens

- **Small screens:** `MobileLayout` is a `DataMobileLayout` enum (Stacked, Scroll, Template);
  a `<MobileTemplate>` slot switches to Template. `MobileBreakpoint` and a column's `HideBelow`
  take Bootstrap breakpoint names as strings ("sm" … "xxl"), as in the doc's markup. Layouts are
  CSS (the Template layout renders the table and the card list and shows one of them), so they
  work in prerendering. What CSS cannot change follows a `matchMedia` watcher
  (`tabBlazor.watchMedia`, exposed as `IsSmallScreen`): inline editing becomes a full-screen
  popup, drawers and the side panel go full screen, row actions collapse into the menu, add,
  refresh and page size move into the toolbar menu, and the pager becomes "‹ 3 / 13 ›".
  Not done: showing hidden (`HideBelow`) columns in the stacked card's details.
- **`EditMode` is nullable:** when not set, editing is inline in the table and a popup in other
  views, on small screens and while virtualized (the doc's "popup is the default when
  virtualized"); an explicit `Inline` stays inline while virtualized.
- **Virtualization** loads the first page through the normal request lifecycle (`PageSize` is the
  fetch size), then Blazor's `Virtualize` reads from an index cache, fetching missing ranges one
  at a time. Sources without a total are treated like cursor sources (infinite scroll). While
  virtualized: the header is always sticky, the footer shows "N rows" instead of a pager, the
  header checkbox selects all matching rows, inline details move to the drawer, and rows are
  forced to exactly `ItemSize` (one line, vertical padding folded into the height).
- **QuickTable's `ResizableColumns` had no behaviour** (a drag-handle element without script).
  DataView's `ResizableColumns` uses a small pointer-drag script (`tabBlazor.attachColumnResize`)
  and keeps widths per column id in `ColumnWidths` for the life of the component (remembering
  them per user is a later feature, as the doc says).
- **Column options:** a column's `ColumnOptions` fragment opens from a small menu button in its
  header (QuickTable's equivalent); phase 5's filter controls use the same place.

## Phase 5: column filters

- **One filter per column.** Filters given through `Filters`, `State`, `SetFiltersAsync` or (next
  step) the URL are sanitized: only filterable columns, allowed operators, values converted to
  the column's type (unconvertible ones dropped), at most one per column.
- **Operators:** each kind has one default (text Contains, number and date Between, bool Equals,
  enum In, Guid Equals; Parts columns Contains). `FilterOperators` lists the allowed ones but
  is intersected with what makes sense for the kind (Greater-than on text is dropped). "Empty"
  and "not empty" are added automatically only for `Nullable<T>` values (string nullability is
  not inspected); list them in `FilterOperators` for strings.
- **Ranges are open-ended:** a from–to range with one end empty filters ≥ or ≤. A date range's
  "to" includes the whole day (23:59:59.9999999) for `DateTime`/`DateTimeOffset` columns.
- **`FilterMode`** on the view is a `DataFilterMode` (Menu, Row) enum; `GetFilter(columnId)` and
  `SetColumnFilterAsync(columnId, filter)` were added to the API; a column's `FilterTemplate`
  gets a `DataFilterContext<TItem>` (Column, Filter, SetAsync).
- **The Filters popup** is a dropdown card on wide screens and a bottom-sheet `Offcanvas` on
  small screens. Text filters wait for the same pause as the search box (`SearchDelay`).
- `In` on text or Guid columns takes comma-separated values in the text box.

## Phase 5: URL state

- **Keys:** `{Id}.q`, `.sort` (`-orderDate`; `none` when a default sort was cleared), `.page`
  (1-based), `.size`, `.f.{columnId}` (`op:value`, `between:from,to` with either end empty,
  `in:a,b`, `isNull`), `.view` (`list`/`table`) and `.current` (the key of the item open in the
  drawer or side panel). Without an `Id` the keys have no prefix.
- **Operators in the URL** use HotChocolate-style tokens (`eq`, `neq`, `contains`, `startsWith`,
  `endsWith`, `gt`, `gte`, `lt`, `lte`, `between`, `in`, `isNull`, `isNotNull`).
- **Reading** happens when the columns are known, before the first load (so prerendering loads
  the right page). The URL wins over `State`/`Filters` parameters; bound `State` and `Filters`
  are told after the first interactive render. Back/forward is handled through
  `NavigationManager.LocationChanged`; the view's own writes are ignored there.
- **Writing** happens on user changes only (not when parameters change). With
  `UrlHistory="Push"`, page, sort and filter changes add a history entry; search changes always
  replace it.
- **Duplicate ids** are detected with a scoped `DataViewUrlRegistry` (registered by
  `AddTabBlazor`), like Tabs' `TabsUrlParameterRegistry`; an id that is not URL-safe is an error.
- **Validation:** sorts must be on sortable columns, filters go through the same sanitizer as
  every other filter, page sizes above 1000 and unparsable pages fall back to the defaults.

## Phase 6: Linq2GraphQL adapter (before the spike on a real screen)

- **Package:** `TabBlazor.Linq2GraphQL` depends on the published `Linq2GraphQL.Client` 1.0.67-prerelease and
  references TabBlazor as a normal dependency (the EF package embeds TabBlazor; this one does not).
- **The page function returns `object`** (`Func<int?, string?, TFilter?, List<TSort>?, object>`), so the doc's
  three type arguments are enough; the connection type is read from the returned query. A wrong return type is a
  clear error on first load.
- **The adapter includes** the connection's nodes (or items), total count and page info itself; nested objects
  the columns show must be included by the page function (the doc's "fetch what the columns show" stays a later idea).
- **Combining filters:** every condition is its own filter object, combined with `and` (the doc's "conditions on one
  field share an object" is not needed for correctness). A Between filter sets `gte` and `lte` on one object.
- **A search word that no column can match** (letters on number columns) returns an empty page without calling
  the server (`HotChocolateConvention.MatchesNothing`).
- **"Empty" filters** use `eq: null`/`neq: null`, which HotChocolate rejects on non-nullable fields; they work on
  nullable fields only. In memory "empty" now also means null (an empty string is a value).
- **Path checks** run on first use per set of columns: sortable, filterable and searchable columns (new flags on
  `DataColumnInfo`) and any column a request sorts or filters by. Safe-mode checks against the live schema
  (`GetSchemaForSafeModeAsync`) are not built yet.
- **Tests** run the shared source kit (now generic over the row type) three times, against cursor, cursor
  without total and offset queries on an in-process HotChocolate server with the kit's seed. The server is a
  small copy shaped like Linq2GraphQL's test server rather than a reference to that repository, so TabBlazor's CI
  does not need a sibling checkout; its client is generated with the official generator.
- **Finding for Linq2GraphQL:** generated code refers to `Linq2GraphQL.Client…` without `global::`, so a client
  generated into a namespace under one that contains a `Linq2GraphQL` namespace (like `TabBlazor.*` with this
  adapter) does not compile. Emitting `global::` in the templates would fix it.

## Phase 7: docs and migration

- **Localizable strings** go through `TablerOptions` rather than a separate provider: `DataViewText` sets
  app-wide strings and `DataViewTextResource` names a resource type read through `IStringLocalizerFactory`
  (keys are the `DataViewText` property names, missing keys stay English, cached per UI culture). A view's
  `Text` parameter still wins. `DataViewText.FromLocalizer` is public for apps with their own localizer.
- **Migration guides** are one docs page (`/docs/data-views/migration`) with a parameter map for Table and one
  for QuickTable, plus notes on the Table and QuickTable pages. No `[Obsolete]` yet, as the doc says.
- The repo has no `AGENTS.md`; only `CLAUDE.md` was updated.
