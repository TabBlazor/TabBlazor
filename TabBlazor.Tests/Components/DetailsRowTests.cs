using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using TabBlazor.Components.Tables;
using TabBlazor.Components.Tables.Components;

namespace TabBlazor.Tests.Components
{
    public class DetailsRowTests : TabBlazorTestContext
    {
        private sealed class FakeDetailsTable : IDetailsTable<string>
        {
            public int VisibleColumnCount { get; set; } = 3;
            public bool ClearCalled { get; private set; }
            public RenderFragment<string> DetailsTemplate { get; set; }

            public Task ClearSelectedItem()
            {
                ClearCalled = true;
                return Task.CompletedTask;
            }
        }

        private static FakeDetailsTable TableWithTemplate(string content) => new FakeDetailsTable
        {
            DetailsTemplate = item => builder => builder.AddContent(0, $"{content}:{item}")
        };

        [Fact]
        public void Renders_row_with_details_cell()
        {
            var table = TableWithTemplate("body");

            var cut = Render<DetailsRow<string>>(p => p
                .Add(r => r.Table, table)
                .Add(r => r.Item, "row1"));

            Assert.NotNull(cut.Find("tr"));
            Assert.NotNull(cut.Find("td.div-table-details"));
        }

        [Fact]
        public void Cell_colspan_matches_visible_column_count()
        {
            var table = TableWithTemplate("body");
            table.VisibleColumnCount = 5;

            var cut = Render<DetailsRow<string>>(p => p
                .Add(r => r.Table, table)
                .Add(r => r.Item, "row1"));

            Assert.Equal("5", cut.Find("td.div-table-details").GetAttribute("colspan"));
        }

        [Fact]
        public void Renders_details_template_with_item()
        {
            var table = TableWithTemplate("body");

            var cut = Render<DetailsRow<string>>(p => p
                .Add(r => r.Table, table)
                .Add(r => r.Item, "row1"));

            Assert.Contains("body:row1", cut.Markup);
        }

        [Fact]
        public void Renders_close_icon()
        {
            var table = TableWithTemplate("body");

            var cut = Render<DetailsRow<string>>(p => p
                .Add(r => r.Table, table)
                .Add(r => r.Item, "row1"));

            Assert.Single(cut.FindAll("i.div-table-details-close"));
        }

        [Fact]
        public void Clicking_close_clears_selected_item()
        {
            var table = TableWithTemplate("body");

            var cut = Render<DetailsRow<string>>(p => p
                .Add(r => r.Table, table)
                .Add(r => r.Item, "row1"));

            cut.Find("i.div-table-details-close").Click();

            Assert.True(table.ClearCalled);
        }
    }
}
