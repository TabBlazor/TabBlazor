namespace TabBlazor.Tests.Components
{
    public class ProgressTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_progress_with_bar()
        {
            var cut = Render<Progress>(p => p.Add(x => x.Percentage, 50));

            Assert.Contains("progress", cut.Find("div.progress").ClassList);
            Assert.Contains("progress-bar", cut.Find("div.progress-bar").ClassList);
        }

        [Fact]
        public void Sets_bar_width_from_percentage()
        {
            var cut = Render<Progress>(p => p.Add(x => x.Percentage, 75));

            var bar = cut.Find("div.progress-bar");
            Assert.Contains("width: 75%", bar.GetAttribute("style"));
            Assert.Equal("75", bar.GetAttribute("aria-valuenow"));
        }

        [Theory]
        [InlineData(ProgressSize.Small, "progress-sm")]
        [InlineData(ProgressSize.Large, "progress-lg")]
        public void Adds_size_class(ProgressSize size, string expected)
        {
            var cut = Render<Progress>(p => p.Add(x => x.Size, size));
            Assert.Contains(expected, cut.Find("div.progress").ClassList);
        }

        [Fact]
        public void Adds_indeterminate_class_to_bar()
        {
            var cut = Render<Progress>(p => p.Add(x => x.Indeterminate, true));
            Assert.Contains("progress-bar-indeterminate", cut.Find("div.progress-bar").ClassList);
        }

        [Fact]
        public void Adds_bar_color_class()
        {
            var cut = Render<Progress>(p => p.Add(x => x.Color, TablerColor.Green));
            Assert.Contains("bg-green", cut.Find("div.progress-bar").ClassList);
        }

        [Fact]
        public void Renders_text_on_bar()
        {
            var cut = Render<Progress>(p => p.Add(x => x.Text, "50%"));
            Assert.Equal("50%", cut.Find("div.progress-bar span").TextContent);
        }
    }
}
