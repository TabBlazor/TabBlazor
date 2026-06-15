using TabBlazor.Components.QuickTables;

namespace TabBlazor.Tests.Components
{
    public class PaginatorTests : TabBlazorTestContext
    {
        [Fact]
        public void Renders_card_footer()
        {
            var cut = Render<Paginator>(p => p.Add(x => x.Value, new PaginationState()));

            Assert.Contains("card-footer", cut.Find("div").ClassList);
        }

        [Fact]
        public void Hides_pagination_when_total_count_unknown()
        {
            var cut = Render<Paginator>(p => p.Add(x => x.Value, new PaginationState()));

            Assert.Empty(cut.FindAll("ul.pagination"));
        }

        [Fact]
        public void Footer_is_empty_when_total_count_unknown()
        {
            var cut = Render<Paginator>(p => p.Add(x => x.Value, new PaginationState()));

            Assert.Empty(cut.Find("div.card-footer").Children);
        }
    }
}
