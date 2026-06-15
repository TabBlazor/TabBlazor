namespace TabBlazor.Tests.Components
{
    public class CardTabsTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_div_with_card_tabs_class()
        {
            var cut = Render<CardTabs>(p => p.AddChildContent("Tabs"));

            var div = cut.Find("div");
            Assert.Contains("card-tabs", div.ClassList);
        }

        [Fact]
        public void Renders_child_content()
        {
            var cut = Render<CardTabs>(p => p.AddChildContent("<span>Inner</span>"));
            Assert.Equal("Inner", cut.Find("span").TextContent);
        }

        [Fact]
        public void Adds_background_color_class()
        {
            var cut = Render<CardTabs>(p => p.Add(c => c.BackgroundColor, TablerColor.Primary));
            Assert.Contains("bg-primary", cut.Find("div").ClassList);
        }
    }
}
