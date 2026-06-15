namespace TabBlazor.Tests.Components
{
    public class PageHeaderTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_div_with_page_header_class()
        {
            var cut = Render<PageHeader>(p => p.AddChildContent("header"));

            var div = cut.Find("div");
            Assert.Contains("page-header", div.ClassList);
            Assert.Contains("header", div.TextContent);
        }

        [Fact]
        public void Renders_child_content()
        {
            var cut = Render<PageHeader>(p => p.AddChildContent("<h1>Title</h1>"));
            Assert.Single(cut.FindAll("h1"));
        }

        [Fact]
        public void Adds_text_color_class()
        {
            var cut = Render<PageHeader>(p => p.Add(x => x.TextColor, TablerColor.Danger));
            Assert.Contains("text-danger", cut.Find("div").ClassList);
        }

        [Fact]
        public void Forwards_unmatched_attributes()
        {
            var cut = Render<PageHeader>(p => p.AddUnmatched("data-test", "value"));
            Assert.Equal("value", cut.Find("div").GetAttribute("data-test"));
        }
    }
}
