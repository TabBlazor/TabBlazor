namespace TabBlazor.Tests.Components
{
    public class CardBodyTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_div_with_card_body_class()
        {
            var cut = Render<CardBody>(p => p.AddChildContent("Content"));

            var div = cut.Find("div");
            Assert.Contains("card-body", div.ClassList);
        }

        [Fact]
        public void Renders_child_content()
        {
            var cut = Render<CardBody>(p => p.AddChildContent("<span>Inner</span>"));
            Assert.Equal("Inner", cut.Find("span").TextContent);
        }

        [Fact]
        public void Adds_background_color_class()
        {
            var cut = Render<CardBody>(p => p.Add(c => c.BackgroundColor, TablerColor.Primary));
            Assert.Contains("bg-primary", cut.Find("div").ClassList);
        }
    }
}
