namespace TabBlazor.Tests.Components
{
    public class IconTests : TabBlazorTestContext
    {
        private static IIconType StrokeIcon => new TablerIcon("<line x1='1' y1='1' x2='2' y2='2' />");
        private static IIconType FilledIcon => new MDIcon("<path d='M1 1' />");

        [Fact]
        public void Renders_svg_with_default_size()
        {
            var cut = Render<Icon>(p => p.Add(i => i.IconType, StrokeIcon));

            var svg = cut.Find("svg");
            Assert.Equal("24", svg.GetAttribute("width"));
            Assert.Equal("24", svg.GetAttribute("height"));
        }

        [Fact]
        public void Renders_stroke_variant_for_unfilled_icon()
        {
            var cut = Render<Icon>(p => p.Add(i => i.IconType, StrokeIcon));

            var svg = cut.Find("svg");
            Assert.Equal("none", svg.GetAttribute("fill"));
            Assert.Equal("currentColor", svg.GetAttribute("stroke"));
        }

        [Fact]
        public void Renders_filled_variant_for_filled_icon()
        {
            var cut = Render<Icon>(p => p.Add(i => i.IconType, FilledIcon));
            Assert.Equal("currentColor", cut.Find("svg").GetAttribute("fill"));
        }

        [Fact]
        public void Applies_custom_size()
        {
            var cut = Render<Icon>(p => p
                .Add(i => i.IconType, StrokeIcon)
                .Add(i => i.Size, 32));

            Assert.Equal("32", cut.Find("svg").GetAttribute("width"));
        }

        [Theory]
        [InlineData(IconAnimation.Pulse, "icon-pulse")]
        [InlineData(IconAnimation.Tada, "icon-tada")]
        [InlineData(IconAnimation.Rotate, "icon-rotate")]
        public void Adds_animation_class(IconAnimation animation, string expected)
        {
            var cut = Render<Icon>(p => p
                .Add(i => i.IconType, StrokeIcon)
                .Add(i => i.Animation, animation));

            Assert.Contains(expected, cut.Find("svg").ClassList);
        }

        [Fact]
        public void Renders_title_element_when_title_set()
        {
            var cut = Render<Icon>(p => p
                .Add(i => i.IconType, StrokeIcon)
                .Add(i => i.Title, "Close"));

            Assert.Equal("Close", cut.Find("svg title").TextContent.Trim());
        }
    }
}
