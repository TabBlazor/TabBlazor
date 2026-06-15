namespace TabBlazor.Tests.Components
{
    public class StatusDotTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_span_with_status_dot_class()
        {
            var cut = Render<StatusDot>(p => p.AddChildContent(""));
            Assert.Contains("status-dot", cut.Find("span").ClassList);
        }

        [Fact]
        public void Adds_animated_class_when_animate()
        {
            var cut = Render<StatusDot>(p => p.Add(s => s.Animate, true));
            Assert.Contains("status-dot-animated", cut.Find("span").ClassList);
        }

        [Fact]
        public void Adds_background_color_class()
        {
            var cut = Render<StatusDot>(p => p.Add(s => s.BackgroundColor, TablerColor.Red));
            Assert.Contains("status-red", cut.Find("span").ClassList);
        }

        [Fact]
        public void Does_not_add_color_class_for_default_color()
        {
            var cut = Render<StatusDot>(p => p.AddChildContent(""));
            Assert.DoesNotContain("status-default", cut.Find("span").ClassList);
        }

        [Fact]
        public void Adds_cursor_pointer_when_click_handler_present()
        {
            var cut = Render<StatusDot>(p => p.Add(s => s.OnClick, _ => { }));
            Assert.Contains("cursor-pointer", cut.Find("span").ClassList);
        }
    }
}
