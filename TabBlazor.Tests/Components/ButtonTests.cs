using Microsoft.AspNetCore.Components;

namespace TabBlazor.Tests.Components
{
    public class ButtonTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_button_element_with_btn_class()
        {
            var cut = Render<Button>(p => p.Add(b => b.Text, "Click"));

            var button = cut.Find("button");
            Assert.Contains("btn", button.ClassList);
            Assert.Contains("Click", button.TextContent);
        }

        [Fact]
        public void Renders_child_content_over_text()
        {
            var cut = Render<Button>(p => p
                .Add(b => b.Text, "Ignored")
                .AddChildContent("<span>Child</span>"));

            Assert.Equal("Child", cut.Find("button").TextContent.Trim());
        }

        [Fact]
        public void Adds_disabled_class_when_disabled()
        {
            var cut = Render<Button>(p => p.Add(b => b.Disabled, true));
            Assert.Contains("disabled", cut.Find("button").ClassList);
        }

        [Theory]
        [InlineData(ButtonSize.Large, "btn-lg")]
        [InlineData(ButtonSize.Small, "btn-sm")]
        public void Adds_size_class(ButtonSize size, string expected)
        {
            var cut = Render<Button>(p => p.Add(b => b.Size, size));
            Assert.Contains(expected, cut.Find("button").ClassList);
        }

        [Theory]
        [InlineData(ButtonShape.Pill, "btn-pill")]
        [InlineData(ButtonShape.Square, "btn-square")]
        public void Adds_shape_class(ButtonShape shape, string expected)
        {
            var cut = Render<Button>(p => p.Add(b => b.Shape, shape));
            Assert.Contains(expected, cut.Find("button").ClassList);
        }

        [Fact]
        public void Renders_anchor_when_type_is_link()
        {
            var cut = Render<Button>(p => p
                .Add(b => b.Type, ButtonType.Link)
                .Add(b => b.LinkTo, "/home"));

            var anchor = cut.Find("a");
            Assert.Equal("/home", anchor.GetAttribute("href"));
        }

        [Fact]
        public void Invokes_on_click()
        {
            var clicked = false;
            var cut = Render<Button>(p => p
                .Add(b => b.OnClick, EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(this, () => clicked = true)));

            cut.Find("button").Click();
            Assert.True(clicked);
        }

        [Fact]
        public void Forwards_unmatched_attributes()
        {
            var cut = Render<Button>(p => p.AddUnmatched("data-test", "value"));
            Assert.Equal("value", cut.Find("button").GetAttribute("data-test"));
        }
    }
}
