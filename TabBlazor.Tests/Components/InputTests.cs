namespace TabBlazor.Tests.Components
{
    public class InputTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_input_element()
        {
            var cut = Render<Input>();

            Assert.Single(cut.FindAll("input"));
        }

        [Fact]
        public void Adds_background_color_class()
        {
            var cut = Render<Input>(p => p.Add(i => i.BackgroundColor, TablerColor.Blue));

            Assert.Contains("bg-blue", cut.Find("input").ClassList);
        }

        [Fact]
        public void Adds_text_color_class()
        {
            var cut = Render<Input>(p => p.Add(i => i.TextColor, TablerColor.Red));

            Assert.Contains("text-red", cut.Find("input").ClassList);
        }

        [Fact]
        public void Forwards_unmatched_attributes()
        {
            var cut = Render<Input>(p => p.AddUnmatched("placeholder", "type here"));

            Assert.Equal("type here", cut.Find("input").GetAttribute("placeholder"));
        }
    }
}
