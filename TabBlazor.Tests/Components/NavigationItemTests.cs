namespace TabBlazor.Tests.Components
{
    public class NavigationItemTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_list_item_with_nav_item_class()
        {
            var cut = Render<NavigationItem>(p => p.Add(n => n.Title, "Home"));

            var li = cut.Find("li");
            Assert.Contains("nav-item", li.ClassList);
            Assert.Contains("cursor-pointer", li.ClassList);
        }

        [Fact]
        public void Renders_title_in_nav_link_title()
        {
            var cut = Render<NavigationItem>(p => p.Add(n => n.Title, "Home"));

            Assert.Equal("Home", cut.Find("span.nav-link-title").TextContent.Trim());
        }

        [Fact]
        public void Renders_child_content_over_title()
        {
            var cut = Render<NavigationItem>(p => p
                .Add(n => n.Title, "Ignored")
                .AddChildContent("<em>Custom</em>"));

            var title = cut.Find("span.nav-link-title");
            Assert.Contains("Custom", title.TextContent);
            Assert.DoesNotContain("Ignored", title.TextContent);
        }

        [Fact]
        public void Renders_anchor_with_nav_link_class()
        {
            var cut = Render<NavigationItem>(p => p.Add(n => n.Title, "Home"));

            Assert.Contains("nav-link", cut.Find("a").ClassList);
        }

        [Fact]
        public void Renders_menu_icon_when_provided()
        {
            var cut = Render<NavigationItem>(p => p
                .Add(n => n.Title, "Home")
                .Add(n => n.MenuIcon, "<i class=\"my-icon\"></i>"));

            Assert.Single(cut.FindAll("span.nav-link-icon i.my-icon"));
        }
    }
}
