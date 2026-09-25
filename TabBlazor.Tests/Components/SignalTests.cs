namespace TabBlazor.Tests.Components
{
    public class SignalTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_three_bars_by_default()
        {
            var cut = Render<Signal>();

            Assert.Contains("signal", cut.Find("span.signal").ClassList);
            Assert.Equal(3, cut.FindAll("span.signal-bar").Count);
        }

        [Fact]
        public void Activates_level_bars()
        {
            var cut = Render<Signal>(p => p.Add(s => s.Level, 2));

            var bars = cut.FindAll("span.signal-bar");
            Assert.Contains("active", bars[0].ClassList);
            Assert.Contains("active", bars[1].ClassList);
            Assert.DoesNotContain("active", bars[2].ClassList);
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(5, 5)]
        [InlineData(9, 5)]
        public void Clamps_bar_count(int bars, int expected)
        {
            var cut = Render<Signal>(p => p.Add(s => s.Bars, bars));
            Assert.Equal(expected, cut.FindAll("span.signal-bar").Count);
        }

        [Fact]
        public void Clamps_level_to_bar_count()
        {
            var cut = Render<Signal>(p => p.Add(s => s.Bars, 4).Add(s => s.Level, 10));
            Assert.Equal(4, cut.FindAll("span.signal-bar.active").Count);
        }

        [Fact]
        public void Adds_color_class()
        {
            var cut = Render<Signal>(p => p.Add(s => s.Color, TablerColor.Green));
            Assert.Contains("signal-green", cut.Find("span.signal").ClassList);
        }

        [Fact]
        public void Sets_default_aria_label()
        {
            var cut = Render<Signal>(p => p.Add(s => s.Bars, 4).Add(s => s.Level, 3));

            var signal = cut.Find("span.signal");
            Assert.Equal("img", signal.GetAttribute("role"));
            Assert.Equal("Level 3 of 4", signal.GetAttribute("aria-label"));
        }

        [Fact]
        public void Hides_from_assistive_technology_when_label_empty()
        {
            var cut = Render<Signal>(p => p.Add(s => s.Label, ""));

            var signal = cut.Find("span.signal");
            Assert.Equal("true", signal.GetAttribute("aria-hidden"));
            Assert.False(signal.HasAttribute("aria-label"));
        }

        [Fact]
        public void Sets_size_variable()
        {
            var cut = Render<Signal>(p => p.Add(s => s.Size, "2rem"));
            Assert.Contains("--tblr-signal-size: 2rem", cut.Find("span.signal").GetAttribute("style"));
        }
    }
}
