namespace TabBlazor.Tests.Components
{
    public class CardFooterTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_div_with_card_footer_class()
        {
            var cut = Render<CardFooter>(p => p.AddChildContent("Footer"));

            var div = cut.Find("div");
            Assert.Contains("card-footer", div.ClassList);
        }

        [Fact]
        public void Renders_child_content()
        {
            var cut = Render<CardFooter>(p => p.AddChildContent("<span>Inner</span>"));
            Assert.Equal("Inner", cut.Find("span").TextContent);
        }

        [Fact]
        public void Adds_text_color_class()
        {
            var cut = Render<CardFooter>(p => p.Add(c => c.TextColor, TablerColor.Secondary));
            Assert.Contains("text-secondary", cut.Find("div").ClassList);
        }
    }
}
