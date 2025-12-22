using Microsoft.AspNetCore.Components.Routing;

namespace TabBlazor;

public partial class Navbar : TablerBaseComponent, IDisposable
{
    private readonly List<NavbarMenuItem> navbarItems = new();
    [Inject] private NavigationManager navigationManager { get; set; }
    [Parameter] public NavbarBackground Background { get; set; }
    [Parameter] public NavbarDirection Direction { get; set; }
    [Parameter] public bool IsExpanded { get; set; } = true;
    [Parameter] public NavLinkMatch? NavLinkMatch { get; set; }

    protected string HtmlTag => "div";

    protected override string ClassNames => ClassBuilder
        .Add("navbar navbar-expand-md")
        .AddIf("navbar-dark", Background == NavbarBackground.Dark)
        .AddIf("navbar-light", Background == NavbarBackground.Light)
        .AddIf("navbar-transparent", Background == NavbarBackground.Transparent)
        .AddIf("navbar-vertical", Direction == NavbarDirection.Vertical)
        .ToString();

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
        if (Direction == NavbarDirection.Horizontal)
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