namespace TabBlazor.Tests.Components
{
    public class AvatarTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_span_with_avatar_class()
        {
            var cut = Render<Avatar>(p => p.AddChildContent("AB"));

            var span = cut.Find("span");
            Assert.Contains("avatar", span.ClassList);
            Assert.Contains("AB", span.TextContent);
        }

        [Fact]
        public void Sets_background_image_style_when_data_provided()
        {
            var cut = Render<Avatar>(p => p.Add(a => a.Data, "https://example.com/img.png"));

            var style = cut.Find("span").GetAttribute("style");
            Assert.Contains("background-image:url('https://example.com/img.png')", style);
        }

        [Theory]
        [InlineData(AvatarSize.Small, "avatar-sm")]
        [InlineData(AvatarSize.Medium, "avatar-md")]
        [InlineData(AvatarSize.Large, "avatar-lg")]
        [InlineData(AvatarSize.ExtraLarge, "avatar-xl")]
        public void Adds_size_class(AvatarSize size, string expected)
        {
            var cut = Render<Avatar>(p => p.Add(a => a.Size, size));
            Assert.Contains(expected, cut.Find("span").ClassList);
        }

        [Theory]
        [InlineData(AvatarRounded.Rounded, "rounded")]
        [InlineData(AvatarRounded.RoundedSmall, "rounded-sm")]
        [InlineData(AvatarRounded.RoundedLarge, "rounded-lg")]
        [InlineData(AvatarRounded.Circle, "rounded-circle")]
        [InlineData(AvatarRounded.None, "rounded-0")]
        public void Adds_rounded_class(AvatarRounded rounded, string expected)
        {
            var cut = Render<Avatar>(p => p.Add(a => a.Rounded, rounded));
            Assert.Contains(expected, cut.Find("span").ClassList);
        }

        [Fact]
        public void Adds_background_color_class()
        {
            var cut = Render<Avatar>(p => p.Add(a => a.BackgroundColor, TablerColor.Primary));
            Assert.Contains("bg-primary-lt", cut.Find("span").ClassList);
        }
    }
}
