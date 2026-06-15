namespace TabBlazor.Tests.Components
{
    public class ButtonListTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_div_with_btn_list_class()
        {
            var cut = Render<ButtonList>(p => p.AddChildContent("buttons"));

            var div = cut.Find("div");
            Assert.Contains("btn-list", div.ClassList);
            Assert.Contains("buttons", div.TextContent);
        }

        [Fact]
        public void Renders_child_content()
        {
            var cut = Render<ButtonList>(p => p.AddChildContent("<button>A</button><button>B</button>"));
            Assert.Equal(2, cut.FindAll("button").Count);
        }

        [Fact]
        public void Adds_text_color_class()
        {
            var cut = Render<ButtonList>(p => p.Add(x => x.TextColor, TablerColor.Primary));
            Assert.Contains("text-primary", cut.Find("div").ClassList);
        }

        [Fact]
        public void Forwards_unmatched_attributes()
        {
            var cut = Render<ButtonList>(p => p.AddUnmatched("data-test", "value"));
            Assert.Equal("value", cut.Find("div").GetAttribute("data-test"));
        }
    }
}
