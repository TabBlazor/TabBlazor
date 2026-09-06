using Microsoft.AspNetCore.Components.Routing;

namespace TabBlazor;

/// <summary>
/// A responsive navigation bar hosting <see cref="NavbarMenuItem"/> entries, supporting horizontal or vertical
/// layout and collapsing below a configurable breakpoint.
/// </summary>
public partial class Navbar : TablerBaseComponent, IDisposable
{
    private readonly List<NavbarMenuItem> navbarItems = new();
    [Inject] private NavigationManager navigationManager { get; set; }
    /// <summary>The navbar background style (dark, light, transparent).</summary>
    [Parameter] public NavbarBackground Background { get; set; }
    /// <summary>Horizontal or vertical layout.</summary>
    [Parameter] public NavbarDirection Direction { get; set; }
    /// <summary>Folds a vertical navbar down to an icon rail. Ignored for horizontal navbars. Defaults to <see cref="NavbarFold.None"/>.</summary>
    [Parameter] public NavbarFold Fold { get; set; } = NavbarFold.None;
    /// <summary>Optional content pinned to the bottom of a vertical navbar, e.g. a user block.</summary>
    [Parameter] public RenderFragment Footer { get; set; }
    /// <summary>Whether the navbar starts expanded. Defaults to true.</summary>
    [Parameter] public bool IsExpanded { get; set; } = true;
    /// <summary>The breakpoint below which the navbar collapses. Defaults to <see cref="PageBreakpoint.Sm"/>.</summary>
    [Parameter] public PageBreakpoint CollapseAt { get; set; } = PageBreakpoint.Sm;
    /// <summary>How active links are matched against the current URL.</summary>
    [Parameter] public NavLinkMatch? NavLinkMatch { get; set; }

    protected string HtmlTag => "div";

    protected override string ClassNames => ClassBuilder
        .Add("navbar")
        .Add(CollapseAt.ToNavbarExpandClass())
        .AddIf("navbar-dark", Background == NavbarBackground.Dark)
        .AddIf("navbar-light", Background == NavbarBackground.Light)
        .AddIf("navbar-transparent", Background == NavbarBackground.Transparent)
        .AddIf("navbar-vertical", Direction == NavbarDirection.Vertical)
        .AddIf("navbar-folded", IsFolded(NavbarFold.Folded))
        .AddIf("navbar-folded-hover", IsFolded(NavbarFold.FoldedHover))
        .ToString();

    protected string Theme => Background == NavbarBackground.Dark ? "dark" : null;

    private bool IsFolded(NavbarFold fold) => Direction == NavbarDirection.Vertical && Fold == fold;

    public bool IsFoldedNavbar => IsFolded(NavbarFold.Folded) || IsFolded(NavbarFold.FoldedHover);

    private bool ClosesSubMenusOnNavigation => Direction == NavbarDirection.Horizontal || IsFoldedNavbar;

    private void OnMouseLeave()
    {
        if (IsFolded(NavbarFold.FoldedHover))
        {
            CloseAll();
        }
    }

    public void Dispose()
    {
        navigationManager.LocationChanged -= LocationChanged;
    }

    protected override void OnInitialized()
    {
        navigationManager.LocationChanged += LocationChanged;
        base.OnInitialized();
    }

    private void LocationChanged(object sender, LocationChangedEventArgs e)
    {
        if (ClosesSubMenusOnNavigation)
        {
            CloseAll();
        }
    }

    public void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }

    public void CloseAll()
    {
        foreach (var item in navbarItems.Where(e => e.IsTopMenuItem))
        {
            item.CloseDropdown();
        }

        StateHasChanged();
    }

    public void AddNavbarMenuItem(NavbarMenuItem item)
    {
        if (!navbarItems.Contains(item))
        {
            navbarItems.Add(item);
        }
    }

    public void RemoveNavbarMenuItem(NavbarMenuItem item)
    {
        if (navbarItems.Contains(item))
        {
            navbarItems.Remove(item);
        }
    }
}

