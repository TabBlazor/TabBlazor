using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace TabBlazor.Tests.Components
{
    public class NavbarTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_div_with_navbar_class()
        {
            var cut = Render<Navbar>(p => p.AddChildContent("menu"));

            var navbar = cut.Find("div.navbar");
            Assert.Contains("navbar", navbar.ClassList);
            Assert.Contains("menu", navbar.TextContent);
        }

        [Fact]
        public void Adds_default_expand_class_for_sm_breakpoint()
        {
            var cut = Render<Navbar>(p => p.AddChildContent("x"));
            Assert.Contains("navbar-expand-md", cut.Find("div.navbar").ClassList);
        }

        [Fact]
        public void Adds_vertical_class_when_direction_vertical()
        {
            var cut = Render<Navbar>(p => p.Add(n => n.Direction, NavbarDirection.Vertical));
            Assert.Contains("navbar-vertical", cut.Find("div.navbar").ClassList);
        }

        [Fact]
        public void Sets_dark_theme_attribute_when_background_dark()
        {
            var cut = Render<Navbar>(p => p.Add(n => n.Background, NavbarBackground.Dark));

            var navbar = cut.Find("div.navbar");
            Assert.Contains("navbar-dark", navbar.ClassList);
            Assert.Equal("dark", navbar.GetAttribute("data-bs-theme"));
        }

        [Fact]
        public void Omits_theme_attribute_when_background_not_dark()
        {
            var cut = Render<Navbar>(p => p.Add(n => n.Background, NavbarBackground.Light));
            Assert.False(cut.Find("div.navbar").HasAttribute("data-bs-theme"));
        }

        [Theory]
        [InlineData(NavbarFold.Folded, "navbar-folded")]
        [InlineData(NavbarFold.FoldedHover, "navbar-folded-hover")]
        public void Adds_fold_class_when_vertical(NavbarFold fold, string expected)
        {
            var cut = Render<Navbar>(p => p
                .Add(n => n.Direction, NavbarDirection.Vertical)
                .Add(n => n.Fold, fold));

            Assert.Contains(expected, cut.Find("div.navbar").ClassList);
        }

        [Fact]
        public void Ignores_fold_when_horizontal()
        {
            var cut = Render<Navbar>(p => p
                .Add(n => n.Direction, NavbarDirection.Horizontal)
                .Add(n => n.Fold, NavbarFold.Folded));

            var classes = cut.Find("div.navbar").ClassList;
            Assert.DoesNotContain("navbar-folded", classes);
            Assert.DoesNotContain("navbar-folded-hover", classes);
        }

        [Fact]
        public void Renders_footer_when_set()
        {
            var cut = Render<Navbar>(p => p.Add(n => n.Footer, "<span>user</span>"));

            var footer = cut.Find("div.navbar-footer");
            Assert.Contains("user", footer.TextContent);
        }

        [Fact]
        public void Omits_footer_when_not_set()
        {
            var cut = Render<Navbar>(p => p.AddChildContent("x"));
            Assert.Empty(cut.FindAll("div.navbar-footer"));
        }

        [Fact]
        public void Renders_toggler_button()
        {
            var cut = Render<Navbar>(p => p.AddChildContent("x"));
            Assert.Single(cut.FindAll("button.navbar-toggler"));
        }

        [Theory]
        [InlineData(NavLinkMatch.All, "Default/all", "/Default/all", true)]
        [InlineData(NavLinkMatch.All, "default/all?q=Order&page=2", "/Default/all?q=Order", true)]
        [InlineData(NavLinkMatch.All, "Default/all/1234", "/Default/all", false)]
        [InlineData(NavLinkMatch.All, "Default/Swegon.Orders", "/Default/all", false)]
        [InlineData(NavLinkMatch.Prefix, "Default/all/1234?q=x", "/Default/all", true)]
        [InlineData(NavLinkMatch.Prefix, "Default/Swegon.Orders.Changes", "/Default/Swegon.Orders", false)]
        public void Marks_menu_item_active_when_href_matches_current_path(NavLinkMatch match, string currentUrl,
            string href, bool expectedActive)
        {
            Services.GetRequiredService<NavigationManager>().NavigateTo(currentUrl);

            var cut = Render<Navbar>(p => p
                .Add(n => n.NavLinkMatch, match)
                .AddChildContent<NavbarMenuItem>(item => item
                    .Add(i => i.Text, "Item")
                    .Add(i => i.Href, href)));

            Assert.Equal(expectedActive, cut.Find("li.nav-item").ClassList.Contains("active"));
        }

        [Fact]
        public void Does_not_mark_menu_item_active_without_nav_link_match()
        {
            Services.GetRequiredService<NavigationManager>().NavigateTo("Default/all");

            var cut = Render<Navbar>(p => p
                .AddChildContent<NavbarMenuItem>(item => item.Add(i => i.Href, "/Default/all")));

            Assert.DoesNotContain("active", cut.Find("li.nav-item").ClassList);
        }
    }
}
