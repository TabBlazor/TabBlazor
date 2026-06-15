namespace TabBlazor.Tests.Components
{
    public class BadgeTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_span_with_badge_class()
        {
            var cut = Render<Badge>(p => p.AddChildContent("5"));

            var span = cut.Find("span");
            Assert.Contains("badge", span.ClassList);
            Assert.Contains("5", span.TextContent);
        }

        [Fact]
        public void Adds_pill_class_for_pill_shape()
        {
            var cut = Render<Badge>(p => p.Add(b => b.Shape, BadgeShape.Pill));
            Assert.Contains("badge-pill", cut.Find("span").ClassList);
        }

        [Fact]
        public void Outline_type_adds_outline_class()
        {
            var cut = Render<Badge>(p => p.Add(b => b.BadgeType, BadgeType.Outline));
            Assert.Contains("badge-outline", cut.Find("span").ClassList);
        }

        [Fact]
        public void Adds_cursor_pointer_when_click_handler_present()
        {
            var cut = Render<Badge>(p => p.Add(b => b.OnClick, _ => { }));
            Assert.Contains("cursor-pointer", cut.Find("span").ClassList);
        }
    }
}
