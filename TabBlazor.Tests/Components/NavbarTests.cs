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
        public void Renders_toggler_button()
        {
            var cut = Render<Navbar>(p => p.AddChildContent("x"));
            Assert.Single(cut.FindAll("button.navbar-toggler"));
        }
    }
}
