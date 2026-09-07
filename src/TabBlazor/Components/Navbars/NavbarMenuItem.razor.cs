using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Threading.Tasks;
using TabBlazor.Services;

namespace TabBlazor
{
    /// <summary>An item within a <see cref="Navbar"/>, optionally a link and/or a container for a sub-menu.</summary>
    public partial class NavbarMenuItem : TablerBaseComponent, IAsyncDisposable
    {
        [CascadingParameter(Name = "Navbar")] Navbar Navbar { get; set; }
        [CascadingParameter(Name = "Parent")] NavbarMenuItem ParentMenuItem { get; set; }

        [Inject] private NavigationManager NavigationManager { get; set; }
        [Inject] private IServiceProvider ServiceProvider { get; set; }

        /// <summary>The navigation URL. When set, the item renders as a link.</summary>
        [Parameter] public string Href { get; set; }
        /// <summary>The item label text.</summary>
        [Parameter] public string Text { get; set; }
        /// <summary>Optional icon content shown before the text.</summary>
        [Parameter] public RenderFragment MenuItemIcon { get; set; }
        /// <summary>Optional nested sub-menu content.</summary>
        [Parameter] public RenderFragment SubMenu { get; set; }
        /// <summary>Whether the sub-menu starts expanded. Defaults to false.</summary>
        [Parameter] public bool Expanded { get; set; }
        /// <summary>When true, a sub-menu can be expanded/collapsed. Defaults to true.</summary>
        [Parameter] public bool Expandable { get; set; } = true;

        public bool IsTopMenuItem => ParentMenuItem == null;

        protected string HtmlTag => "li";
        protected bool isExpanded;
        private ElementReference itemElement;
        private ElementReference subMenuElement;
        private IPopperService popperService;
        private IPopperInstance flyoutPopper;
        private bool refreshFlyoutPopper;
      
        protected bool IsDropdown => SubMenu != null && Expandable;

        protected bool isSubMenu => ParentMenuItem != null;

        protected override void OnInitialized()
        {
            isExpanded = Expanded;
            Navbar?.AddNavbarMenuItem(this);
            popperService = ServiceProvider.GetService(typeof(IPopperService)) as IPopperService;

            NavigationManager.LocationChanged += LocationChanged;

        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!UsesFlyoutPopper || !isExpanded)
            {
                await DisposeFlyoutPopperAsync();
                return;
            }

            if (flyoutPopper == null)
            {
                flyoutPopper = await popperService.CreateAsync(itemElement, subMenuElement, new PopperOptions
                {
                    Placement = Placement.RightStart,
                    Strategy = Positioning.Fixed,
                    Offset = 4
                });
                await flyoutPopper.ShowAsync();
            }
            else if (refreshFlyoutPopper)
            {
                refreshFlyoutPopper = false;
                await flyoutPopper.UpdateAsync();
            }
        }

        private bool UsesFlyoutPopper =>
            popperService != null
            && IsDropdown
            && IsTopMenuItem
            && Navbar?.Direction == NavbarDirection.Vertical
            && Navbar.Fold == NavbarFold.Folded;

        private async Task DisposeFlyoutPopperAsync()
        {
            if (flyoutPopper == null)
            {
                return;
            }

            var instanceToDispose = flyoutPopper;
            flyoutPopper = null;
            await instanceToDispose.DisposeAsync();
        }

        private void LocationChanged(object sender, LocationChangedEventArgs e)
        {
            StateHasChanged();
        }

        private bool IsActive()
        {
            if (Href == null) { return false; }

            if (Navbar.NavLinkMatch == null) { return false; }

            var navLinkMatch = (NavLinkMatch)Navbar.NavLinkMatch;

            var relativePath = NavigationManager.ToBaseRelativePath(NavigationManager.Uri).ToLower();
            return navLinkMatch == NavLinkMatch.All ? relativePath == Href.ToLower() : relativePath.StartsWith(Href.ToLower());
        }


        private bool NavbarIsHorizontalAndDark => Navbar?.Background == NavbarBackground.Dark && Navbar?.Direction == NavbarDirection.Horizontal;

        private bool isDropEnd => Navbar.Direction == NavbarDirection.Horizontal && ParentMenuItem?.IsDropdown == true;

        protected override string ClassNames => ClassBuilder
            .Add("nav-item")
            .Add("cursor-pointer")
            .AddIf("dropdown", IsDropdown && !isDropEnd)
            .AddIf("dropend", IsDropdown && isDropEnd)
            .AddIf("active", IsActive())
            .ToString();

        public void CloseDropdown()
        {
            isExpanded = false;
        }

        public void ToogleDropdown()
        {
            var expand = !isExpanded;

            if (expand && IsTopMenuItem)
            {
                Navbar.CloseAll();
            }

            isExpanded = expand;
            ParentMenuItem?.OnChildMenuToggled();
        }

        private void OnChildMenuToggled()
        {
            if (!IsTopMenuItem)
            {
                ParentMenuItem?.OnChildMenuToggled();
                return;
            }

            if (flyoutPopper != null)
            {
                refreshFlyoutPopper = true;
                StateHasChanged();
            }
        }

        public async ValueTask DisposeAsync()
        {
            Navbar?.RemoveNavbarMenuItem(this);

            if (NavigationManager != null)
            {
                NavigationManager.LocationChanged -= LocationChanged;
            }

            await DisposeFlyoutPopperAsync();
        }
    }
}

