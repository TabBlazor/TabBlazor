using Microsoft.Extensions.DependencyInjection;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace TabBlazor.Tests.Components
{
    public class NavbarMenuItemTests : TabBlazorTestContext
    {
        private static RenderFragment Group(bool expandWhenActive = true) => builder =>
        {
            builder.OpenComponent<NavbarMenuItem>(0);
            builder.AddAttribute(1, nameof(NavbarMenuItem.Text), "Components");
            builder.AddAttribute(2, nameof(NavbarMenuItem.ExpandWhenActive), expandWhenActive);
            builder.AddAttribute(3, nameof(NavbarMenuItem.SubMenu), (RenderFragment)(sub =>
            {
                sub.OpenComponent<NavbarMenuItem>(0);
                sub.AddAttribute(1, nameof(NavbarMenuItem.Text), "Forms");
                sub.AddAttribute(2, nameof(NavbarMenuItem.SubMenu), (RenderFragment)(inner =>
                {
                    inner.OpenComponent<NavbarMenuItem>(0);
                    inner.AddAttribute(1, nameof(NavbarMenuItem.Text), "Checkboxes");
                    inner.AddAttribute(2, nameof(NavbarMenuItem.Href), "docs/forms/checkboxes");
                    inner.CloseComponent();
                }));
                sub.CloseComponent();
                sub.OpenComponent<NavbarMenuItem>(10);
                sub.AddAttribute(11, nameof(NavbarMenuItem.Text), "Cards");
                sub.AddAttribute(12, nameof(NavbarMenuItem.Href), "docs/cards");
                sub.CloseComponent();
            }));
            builder.CloseComponent();
        };

        private IRenderedComponent<Navbar> RenderNavbar(string url, NavbarDirection direction = NavbarDirection.Vertical, NavbarFold fold = NavbarFold.None, bool expandWhenActive = true)
        {
            var navigation = Services.GetRequiredService<BunitNavigationManager>();
            navigation.NavigateTo(url);

            return Render<Navbar>(p => p
                .Add(n => n.Direction, direction)
                .Add(n => n.Fold, fold)
                .Add(n => n.NavLinkMatch, NavLinkMatch.All)
                .AddChildContent(Group(expandWhenActive)));
        }

        private static bool GroupIsOpen(IRenderedComponent<Navbar> cut, string text) =>
            cut.Find($"li.nav-item:has(> a > span:contains('{text}')) > ul.dropdown-menu").ClassList.Contains("show");

        [Fact]
        public void Expands_group_containing_current_url_on_load()
        {
            var cut = RenderNavbar("docs/cards");

            Assert.True(GroupIsOpen(cut, "Components"));
            Assert.Contains("active", cut.Find("li.nav-item:has(> a[href='docs/cards'])").ClassList);
        }

        [Fact]
        public void Expands_nested_groups_containing_current_url()
        {
            var cut = RenderNavbar("docs/forms/checkboxes");

            Assert.True(GroupIsOpen(cut, "Components"));
            Assert.True(GroupIsOpen(cut, "Forms"));
        }

        [Fact]
        public void Root_href_is_active_only_at_root()
        {
            var navigation = Services.GetRequiredService<BunitNavigationManager>();
            navigation.NavigateTo("");
            var cut = Render<Navbar>(p => p
                .Add(n => n.NavLinkMatch, NavLinkMatch.All)
                .AddChildContent(b =>
                {
                    b.OpenComponent<NavbarMenuItem>(0);
                    b.AddAttribute(1, nameof(NavbarMenuItem.Text), "Home");
                    b.AddAttribute(2, nameof(NavbarMenuItem.Href), "/");
                    b.CloseComponent();
                }));

            Assert.Contains("active", cut.Find("li.nav-item").ClassList);

            cut.InvokeAsync(() => navigation.NavigateTo("docs/cards"));

            cut.WaitForAssertion(() => Assert.DoesNotContain("active", cut.Find("li.nav-item").ClassList));
        }

        [Fact]
        public void Keeps_group_collapsed_when_url_is_elsewhere()
        {
            var cut = RenderNavbar("dashboard");
            Assert.False(GroupIsOpen(cut, "Components"));
        }

        [Fact]
        public void Expands_group_when_navigating_to_child_url()
        {
            var cut = RenderNavbar("dashboard");
            Assert.False(GroupIsOpen(cut, "Components"));

            var navigation = Services.GetRequiredService<BunitNavigationManager>();
            cut.InvokeAsync(() => navigation.NavigateTo("docs/cards"));

            cut.WaitForAssertion(() => Assert.True(GroupIsOpen(cut, "Components")));
        }

        [Fact]
        public void Does_not_expand_when_disabled()
        {
            var cut = RenderNavbar("docs/cards", expandWhenActive: false);
            Assert.False(GroupIsOpen(cut, "Components"));
        }

        [Fact]
        public void Does_not_expand_horizontal_navbar()
        {
            var cut = RenderNavbar("docs/cards", NavbarDirection.Horizontal);
            Assert.False(GroupIsOpen(cut, "Components"));
        }

        [Fact]
        public void Does_not_expand_folded_navbar()
        {
            var cut = RenderNavbar("docs/cards", fold: NavbarFold.Folded);
            Assert.False(GroupIsOpen(cut, "Components"));
        }
    }
}
