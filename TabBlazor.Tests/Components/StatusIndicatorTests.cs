namespace TabBlazor.Tests.Components
{
    public class StatusIndicatorTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_span_with_status_indicator_class()
        {
            var cut = Render<StatusIndicator>(p => p.AddChildContent(""));
            Assert.Contains("status-indicator", cut.Find("span").ClassList);
        }

        [Fact]
        public void Renders_three_indicator_circles()
        {
            var cut = Render<StatusIndicator>(p => p.AddChildContent(""));
            Assert.Equal(3, cut.FindAll("span.status-indicator-circle").Count);
        }

        [Fact]
        public void Adds_animated_class_when_animate()
        {
            var cut = Render<StatusIndicator>(p => p.Add(s => s.Animate, true));
            Assert.Contains("status-indicator-animated", cut.Find("span.status-indicator").ClassList);
        }

        [Fact]
        public void Adds_background_color_class()
        {
            var cut = Render<StatusIndicator>(p => p.Add(s => s.BackgroundColor, TablerColor.Blue));
            Assert.Contains("status-blue", cut.Find("span.status-indicator").ClassList);
        }

        [Fact]
        public void Adds_cursor_pointer_when_click_handler_present()
        {
            var cut = Render<StatusIndicator>(p => p.Add(s => s.OnClick, _ => { }));
            Assert.Contains("cursor-pointer", cut.Find("span.status-indicator").ClassList);
        }
    }
}
