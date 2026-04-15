# CLAUDE.md

This file = guidance for Claude Code (claude.ai/code) working in this repo.

## Project Overview

TabBlazor = Blazor component library on [Tabler](https://preview.tabler.io/) CSS framework (Bootstrap-based admin dashboard UI). 90+ Razor components, minimal JS interop. NuGet packages: `TabBlazor` and `TabBlazor.QuickTable.EntityFramework`.

## Build & Development Commands

```bash
# Build entire solution
dotnet build TabBlazor.slnx --configuration Release

# Run tests (xUnit)
dotnet test
dotnet test --no-build --configuration Release    # after building

# Run a single test
dotnet test --filter "FullyQualifiedName~RenderComponentTests.RenderComponent_can_render_when_setting_parameters"

# Run demo site (WebAssembly)
cd docs/Tabler.Docs.Wasm && dotnet run

# Pack NuGet packages
dotnet pack src/TabBlazor/TabBlazor.csproj -c Release
dotnet pack src/TabBlazor.QuickTable.EntityFramework/TabBlazor.QuickTable.EntityFramework.csproj -c Release
```

**SDK**: .NET 9.0 (pinned in `global.json`, rollForward: latestMinor)
**Versioning**: Nerdbank.GitVersioning (`version.json`, currently 0.14-beta)

## Architecture

### Component Base Pattern

All components inherit from `TablerBaseComponent` (`src/TabBlazor/Components/TablerBaseComponent.cs`):
- Provides `TextColor`, `BackgroundColor` (TablerColor enum), `ChildContent`, `OnClick`
- `CaptureUnmatchedValues` for HTML attribute passthrough
- `ClassBuilder` property for fluent CSS class composition

Components override `ClassNames` to build CSS classes. `ClassBuilder` merges component classes with user-provided `class` attribute.

### Service Registration

```csharp
services.AddTabBlazor(options => { })
    .AddValidation<CustomValidator>();  // optional custom IFormValidator
```

Registers: `TablerService` (JS interop), `IModalService`, `IOffcanvasService`, `ToastService`, `TableFilterService`, `IFormValidator` (default: DataAnnotations), `FlagService`.

### Key Component Categories

- **QuickTable** (`Components/QuickTables/`): Virtual scrolling table with `GridItemsProvider<T>`, `PropertyColumn`, `TemplateColumn`, sort/filter/group. EF Core integration in separate package.
- **Table** (`Components/Tables/`): Standard table with editable rows, detail rows, grouping, column config.
- **Forms** (`Components/Forms/`): `TablerForm` with EditContext, built-in inputs (TextInput, Select, Autocomplete, Typeahead, Datepicker, etc.), pluggable validation via `IFormValidator`.
- **Modals** (`Components/Modals/`): `IModalService.ShowAsync()` pattern with `ModalResult` returns.
- **Navigation** (`Components/Navbars/`, `Components/Navigation/`): Navbar with responsive collapse, sidebar, tabs.

### JavaScript Interop

Minimal JS in `src/TabBlazor/wwwroot/js/tabblazor.js` (~200 lines). All interop through `TablerService`. Handles: theme switching, clipboard, file downloads, scrolling, element manipulation.

### Styling

Tabler CSS framework. Custom styles in `src/TabBlazor/wwwroot/css/tabblazor.scss`. Color system via `TablerColor` enum with `GetColorClass()` extensions for btn/bg/text variants.

## Project Layout

- `src/TabBlazor/` — Main component library (NuGet package)
- `src/TabBlazor.QuickTable.EntityFramework/` — EF Core integration for QuickTable
- `docs/Tabler.Docs/` — Shared demo page components (Razor class library)
- `docs/Tabler.Docs.Wasm/` — WebAssembly demo site (deployed to GitHub Pages)
- `TabBlazor.Tests/` — xUnit tests
- `Icons/IconGenerator/` — Tool to generate icon components

## CI/CD

- PRs: build + test (`ci-pr.yml`, windows-2022 runner)
- Master push: build + test + publish docs to GitHub Pages (`ci.yml`)
- Releases: manual workflow packs + pushes to NuGet (`create-release.yml`)

## Commit Messages

* **Format:**
  ```
  <short summary>

  [optional body]
  ```
* **NEVER use Conventional Commits prefixes** such as `feat:`, `fix:`, `chore:`, `api:`, `refactor:`, `docs:`, `style:`, `test:`, `build:`, `ci:`, or any other `<type>:` prefix. The summary must start directly with an imperative verb.
* **Summary:** Imperative mood, lowercase, no trailing period. Max ~72 characters.
* **Body:** Explain *why*, not *what*. Wrap at 72 characters.

**Examples:**
```
Add customer order filter by delivery date

Resolve null reference in pricing resolver

Extract M3 event parsing to separate class

Add EF migration for contact email field
```
