namespace TabBlazor.Tests.Components
{
    public class TimelineItemTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_li_with_timeline_event_class()
        {
            var cut = Render<TimelineItem>(p => p.AddChildContent("body"));

            var item = cut.Find("li");
            Assert.Contains("timeline-event", item.ClassList);
            Assert.Contains("body", item.TextContent);
        }

        [Fact]
        public void Renders_title_in_heading()
        {
            var cut = Render<TimelineItem>(p => p.Add(i => i.Title, "Deployed"));
            Assert.Equal("Deployed", cut.Find("h4").TextContent);
        }

        [Fact]
        public void Omits_heading_when_title_empty()
        {
            var cut = Render<TimelineItem>(p => p.AddChildContent("body"));
            Assert.Empty(cut.FindAll("h4"));
        }

        [Fact]
        public void Renders_time_label()
        {
            var cut = Render<TimelineItem>(p => p.Add(i => i.Time, "10:00"));
            Assert.Contains("10:00", cut.Find("div.float-end").TextContent);
        }

        [Fact]
        public void Renders_icon_text()
        {
            var cut = Render<TimelineItem>(p => p.Add(i => i.IconText, "A"));
            Assert.Contains("A", cut.Find("div.timeline-event-icon").TextContent);
        }

        [Fact]
        public void Adds_icon_color_class_to_icon()
        {
            var cut = Render<TimelineItem>(p => p.Add(i => i.IconColor, TablerColor.Green));
            Assert.Contains("bg-green-lt", cut.Find("div.timeline-event-icon").ClassList);
        }
    }
}
