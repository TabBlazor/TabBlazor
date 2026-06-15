namespace TabBlazor.Tests.Components
{
    public class PageMainTitleTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_h1_with_page_title_class_by_default()
        {
            var cut = Render<PageMainTitle>(p => p.AddChildContent("Dashboard"));

            var heading = cut.Find("h1");
            Assert.Contains("page-title", heading.ClassList);
            Assert.Equal("Dashboard", heading.TextContent);
        }

        [Fact]
        public void Renders_custom_html_tag()
        {
            var cut = Render<PageMainTitle>(p => p
                .Add(x => x.HtmlTag, "h2")
                .AddChildContent("Sub"));

            Assert.Single(cut.FindAll("h2"));
            Assert.Empty(cut.FindAll("h1"));
        }

        [Fact]
        public void Adds_text_color_class()
        {
            var cut = Render<PageMainTitle>(p => p.Add(x => x.TextColor, TablerColor.Success));
            Assert.Contains("text-success", cut.Find("h1").ClassList);
        }

        [Fact]
        public void Forwards_unmatched_attributes()
        {
            var cut = Render<PageMainTitle>(p => p.AddUnmatched("data-test", "value"));
            Assert.Equal("value", cut.Find("h1").GetAttribute("data-test"));
        }
    }
}
