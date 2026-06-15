namespace TabBlazor.Tests.Components
{
    public class PagePretitleTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_div_with_page_pretitle_class()
        {
            var cut = Render<PagePretitle>(p => p.AddChildContent("Overview"));

            var div = cut.Find("div");
            Assert.Contains("page-pretitle", div.ClassList);
            Assert.Equal("Overview", div.TextContent);
        }

        [Fact]
        public void Renders_child_content()
        {
            var cut = Render<PagePretitle>(p => p.AddChildContent("<em>note</em>"));
            Assert.Single(cut.FindAll("em"));
        }

        [Fact]
        public void Adds_background_color_class()
        {
            var cut = Render<PagePretitle>(p => p.Add(x => x.BackgroundColor, TablerColor.Azure));
            Assert.Contains("bg-azure", cut.Find("div").ClassList);
        }

        [Fact]
        public void Forwards_unmatched_attributes()
        {
            var cut = Render<PagePretitle>(p => p.AddUnmatched("data-test", "value"));
            Assert.Equal("value", cut.Find("div").GetAttribute("data-test"));
        }
    }
}
