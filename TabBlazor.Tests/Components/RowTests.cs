namespace TabBlazor.Tests.Components
{
    public class RowTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_div_with_row_class()
        {
            var cut = Render<Row>(p => p.AddChildContent("content"));

            var div = cut.Find("div");
            Assert.Contains("row", div.ClassList);
            Assert.Contains("content", div.TextContent);
        }

        [Fact]
        public void Renders_child_content()
        {
            var cut = Render<Row>(p => p.AddChildContent("<span>Cell</span>"));
            Assert.Single(cut.FindAll("span"));
        }

        [Fact]
        public void Adds_row_cards_class_when_has_cards()
        {
            var cut = Render<Row>(p => p.Add(r => r.HasCards, true));
            Assert.Contains("row-cards", cut.Find("div").ClassList);
        }

        [Fact]
        public void Omits_row_cards_class_by_default()
        {
            var cut = Render<Row>(p => p.AddChildContent("x"));
            Assert.DoesNotContain("row-cards", cut.Find("div").ClassList);
        }

        [Fact]
        public void Forwards_unmatched_attributes()
        {
            var cut = Render<Row>(p => p.AddUnmatched("data-test", "value"));
            Assert.Equal("value", cut.Find("div").GetAttribute("data-test"));
        }
    }
}
