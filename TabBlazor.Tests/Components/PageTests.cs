namespace TabBlazor.Tests.Components
{
    public class PageTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_div_with_page_class()
        {
            var cut = Render<Page>(p => p.AddChildContent("body"));

            var div = cut.Find("div");
            Assert.Contains("page", div.ClassList);
            Assert.Contains("body", div.TextContent);
        }

        [Fact]
        public void Renders_child_content()
        {
            var cut = Render<Page>(p => p.AddChildContent("<section>Inner</section>"));
            Assert.Single(cut.FindAll("section"));
        }

        [Fact]
        public void Adds_background_color_class()
        {
            var cut = Render<Page>(p => p.Add(x => x.BackgroundColor, TablerColor.Primary));
            Assert.Contains("bg-primary", cut.Find("div").ClassList);
        }

        [Fact]
        public void Forwards_unmatched_attributes()
        {
            var cut = Render<Page>(p => p.AddUnmatched("data-test", "value"));
            Assert.Equal("value", cut.Find("div").GetAttribute("data-test"));
        }
    }
}
