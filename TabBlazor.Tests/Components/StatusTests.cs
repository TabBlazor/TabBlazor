namespace TabBlazor.Tests.Components
{
    public class StatusTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_span_with_status_class()
        {
            var cut = Render<Status>(p => p.AddChildContent("Active"));

            var span = cut.Find("span");
            Assert.Contains("status", span.ClassList);
            Assert.Contains("Active", span.TextContent);
        }

        [Fact]
        public void Adds_background_color_class()
        {
            var cut = Render<Status>(p => p.Add(s => s.BackgroundColor, TablerColor.Green));
            Assert.Contains("status-green", cut.Find("span").ClassList);
        }

        [Fact]
        public void Adds_lite_class_when_lite()
        {
            var cut = Render<Status>(p => p.Add(s => s.Lite, true));
            Assert.Contains("status-lite", cut.Find("span").ClassList);
        }

        [Fact]
        public void Adds_cursor_pointer_when_click_handler_present()
        {
            var cut = Render<Status>(p => p.Add(s => s.OnClick, _ => { }));
            Assert.Contains("cursor-pointer", cut.Find("span").ClassList);
        }

        [Fact]
        public void Renders_status_dot_when_dot_type_set()
        {
            var cut = Render<Status>(p => p.Add(s => s.DotType, StatusDotType.Normal));
            Assert.Contains("status-dot", cut.Find("span span").ClassList);
        }

        [Fact]
        public void Renders_animated_dot_when_dot_type_animate()
        {
            var cut = Render<Status>(p => p.Add(s => s.DotType, StatusDotType.Animate));
            Assert.Contains("status-dot-animated", cut.Find("span span").ClassList);
        }
    }
}
