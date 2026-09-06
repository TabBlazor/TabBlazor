namespace TabBlazor.Tests.Components
{
    public class CardTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_div_with_card_class()
        {
            var cut = Render<Card>(p => p.AddChildContent("Body"));

            var div = cut.Find("div.card");
            Assert.Contains("card", div.ClassList);
            Assert.Contains("Body", cut.Markup);
        }

        [Fact]
        public void Renders_anchor_when_link_to_set()
        {
            var cut = Render<Card>(p => p.Add(c => c.LinkTo, "/home"));

            var anchor = cut.Find("a");
            Assert.Equal("/home", anchor.GetAttribute("href"));
        }

        [Fact]
        public void Adds_stacked_class_when_stacked()
        {
            var cut = Render<Card>(p => p.Add(c => c.Stacked, true));
            Assert.Contains("card-stacked", cut.Find("div.card").ClassList);
        }

        [Theory]
        [InlineData(CardSize.Small, "card-sm")]
        [InlineData(CardSize.Medium, "card-md")]
        [InlineData(CardSize.Large, "card-lg")]
        public void Adds_size_class(CardSize size, string expected)
        {
            var cut = Render<Card>(p => p.Add(c => c.Size, size));
            Assert.Contains(expected, cut.Find("div.card").ClassList);
        }

        [Fact]
        public void Adds_gradient_classes_when_gradient_set()
        {
            var cut = Render<Card>(p => p.Add(c => c.Gradient, TablerColor.Primary));

            var classes = cut.Find("div.card").ClassList;
            Assert.Contains("card-gradient", classes);
            Assert.Contains("card-gradient-primary", classes);
        }

        [Fact]
        public void Omits_gradient_classes_by_default()
        {
            var cut = Render<Card>(p => p.Add(c => c.GradientDirection, CardGradientDirection.End).Add(c => c.GradientAnimated, true));

            var classes = cut.Find("div.card").ClassList;
            Assert.DoesNotContain("card-gradient", classes);
            Assert.DoesNotContain("card-gradient-end", classes);
            Assert.DoesNotContain("card-gradient-animated", classes);
        }

        [Theory]
        [InlineData(CardGradientDirection.Start, "card-gradient-start")]
        [InlineData(CardGradientDirection.End, "card-gradient-end")]
        [InlineData(CardGradientDirection.Bottom, "card-gradient-bottom")]
        public void Adds_gradient_direction_class(CardGradientDirection direction, string expected)
        {
            var cut = Render<Card>(p => p.Add(c => c.Gradient, TablerColor.Green).Add(c => c.GradientDirection, direction));
            Assert.Contains(expected, cut.Find("div.card").ClassList);
        }

        [Fact]
        public void Adds_animated_class_when_gradient_animated()
        {
            var cut = Render<Card>(p => p.Add(c => c.Gradient, TablerColor.Green).Add(c => c.GradientAnimated, true));
            Assert.Contains("card-gradient-animated", cut.Find("div.card").ClassList);
        }

        [Fact]
        public void Adds_dashed_class_when_dashed()
        {
            var cut = Render<Card>(p => p.Add(c => c.Dashed, true));
            Assert.Contains("card-dashed", cut.Find("div.card").ClassList);
        }

        [Fact]
        public void Adds_transparent_class_when_transparent()
        {
            var cut = Render<Card>(p => p.Add(c => c.Transparent, true));
            Assert.Contains("card-transparent", cut.Find("div.card").ClassList);
        }

        [Fact]
        public void Renders_top_status_bar_when_status_top_set()
        {
            var cut = Render<Card>(p => p.Add(c => c.StatusTop, TablerColor.Primary));

            var status = cut.Find("div.card-status-top");
            Assert.Contains("bg-primary", status.ClassList);
        }

        [Fact]
        public void Renders_start_status_bar_when_status_start_set()
        {
            var cut = Render<Card>(p => p.Add(c => c.StatusStart, TablerColor.Danger));

            var status = cut.Find("div.card-status-start");
            Assert.Contains("bg-danger", status.ClassList);
        }
    }
}
