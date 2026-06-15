namespace TabBlazor.Tests.Components
{
    public class CardTitleTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_h1_with_card_title_class_by_default()
        {
            var cut = Render<CardTitle>(p => p.AddChildContent("My title"));

            var heading = cut.Find("h1");
            Assert.Contains("card-title", heading.ClassList);
            Assert.Equal("My title", heading.TextContent);
        }

        [Fact]
        public void Renders_custom_html_tag()
        {
            var cut = Render<CardTitle>(p => p
                .Add(c => c.HtmlTag, "h3")
                .AddChildContent("Title"));

            var heading = cut.Find("h3");
            Assert.Contains("card-title", heading.ClassList);
        }

        [Fact]
        public void Adds_text_color_class()
        {
            var cut = Render<CardTitle>(p => p.Add(c => c.TextColor, TablerColor.Primary));
            Assert.Contains("text-primary", cut.Find("h1").ClassList);
        }
    }
}
