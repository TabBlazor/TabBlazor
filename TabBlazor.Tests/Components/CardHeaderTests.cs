namespace TabBlazor.Tests.Components
{
    public class CardHeaderTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_div_with_card_header_class()
        {
            var cut = Render<CardHeader>(p => p.AddChildContent("Header"));

            var div = cut.Find("div");
            Assert.Contains("card-header", div.ClassList);
        }

        [Fact]
        public void Renders_child_content()
        {
            var cut = Render<CardHeader>(p => p.AddChildContent("<span>Inner</span>"));
            Assert.Equal("Inner", cut.Find("span").TextContent);
        }

        [Fact]
        public void Adds_background_color_class()
        {
            var cut = Render<CardHeader>(p => p.Add(c => c.BackgroundColor, TablerColor.Dark));
            Assert.Contains("bg-dark", cut.Find("div").ClassList);
        }
    }
}
