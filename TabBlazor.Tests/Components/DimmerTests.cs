namespace TabBlazor.Tests.Components
{
    public class DimmerTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_dimmer_wrapper_when_active()
        {
            var cut = Render<Dimmer>(p => p
                .Add(d => d.Active, true)
                .AddChildContent("<span>content</span>"));

            var div = cut.Find("div.dimmer");
            Assert.Contains("dimmer", div.ClassList);
            Assert.Contains("active", div.ClassList);
        }

        [Fact]
        public void Renders_only_child_content_when_inactive()
        {
            var cut = Render<Dimmer>(p => p
                .Add(d => d.Active, false)
                .AddChildContent("<span>content</span>"));

            Assert.Empty(cut.FindAll("div.dimmer"));
            Assert.Equal("content", cut.Find("span").TextContent);
        }

        [Fact]
        public void Renders_spinner_by_default_when_active()
        {
            var cut = Render<Dimmer>(p => p.Add(d => d.Active, true));
            Assert.Single(cut.FindAll("div.loader"));
        }

        [Fact]
        public void Hides_spinner_when_show_spinner_false()
        {
            var cut = Render<Dimmer>(p => p
                .Add(d => d.Active, true)
                .Add(d => d.ShowSpinner, false));

            Assert.Empty(cut.FindAll("div.loader"));
        }

        [Fact]
        public void Wraps_child_content_in_dimmer_content_when_active()
        {
            var cut = Render<Dimmer>(p => p
                .Add(d => d.Active, true)
                .AddChildContent("<span>inner</span>"));

            Assert.Equal("inner", cut.Find("div.dimmer-content span").TextContent);
        }
    }
}
