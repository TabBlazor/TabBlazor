namespace TabBlazor.Tests.Components
{
    public class TimelineTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_ul_with_timeline_class()
        {
            var cut = Render<Timeline>(p => p.AddChildContent("events"));

            var list = cut.Find("ul");
            Assert.Contains("timeline", list.ClassList);
            Assert.Contains("events", list.TextContent);
        }

        [Fact]
        public void Renders_child_content()
        {
            var cut = Render<Timeline>(p => p.AddChildContent("<li>Event</li>"));
            Assert.Single(cut.FindAll("li"));
        }

        [Fact]
        public void Adds_timeline_simple_class_when_type_is_simple()
        {
            var cut = Render<Timeline>(p => p.Add(t => t.Type, TimelineType.Simple));
            Assert.Contains("timeline-simple", cut.Find("ul").ClassList);
        }

        [Fact]
        public void Omits_timeline_simple_class_by_default()
        {
            var cut = Render<Timeline>(p => p.AddChildContent("x"));
            Assert.DoesNotContain("timeline-simple", cut.Find("ul").ClassList);
        }

        [Fact]
        public void Forwards_unmatched_attributes()
        {
            var cut = Render<Timeline>(p => p.AddUnmatched("data-test", "value"));
            Assert.Equal("value", cut.Find("ul").GetAttribute("data-test"));
        }
    }
}
