namespace TabBlazor.Tests.Components
{
    public class CardRibbonTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_div_with_ribbon_class()
        {
            var cut = Render<CardRibbon>(p => p.AddChildContent("New"));

            var div = cut.Find("div");
            Assert.Contains("ribbon", div.ClassList);
            Assert.Contains("New", cut.Markup);
        }

        [Fact]
        public void Defaults_to_ribbon_right()
        {
            var cut = Render<CardRibbon>(p => p.AddChildContent("New"));
            Assert.Contains("ribbon-right", cut.Find("div").ClassList);
        }

        [Theory]
        [InlineData(RibbonPosition.Top, "ribbon-top")]
        [InlineData(RibbonPosition.Right, "ribbon-right")]
        public void Adds_position_class(RibbonPosition position, string expected)
        {
            var cut = Render<CardRibbon>(p => p.Add(c => c.Position, position));
            Assert.Contains(expected, cut.Find("div").ClassList);
        }

        [Fact]
        public void Adds_background_color_class()
        {
            var cut = Render<CardRibbon>(p => p.Add(c => c.BackgroundColor, TablerColor.Green));
            Assert.Contains("bg-green", cut.Find("div").ClassList);
        }
    }
}
