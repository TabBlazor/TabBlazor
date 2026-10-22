using System.Collections.Generic;
using TabBlazor.Components.Tables.Components;

namespace TabBlazor.Tests.Components
{
    public class TableTests : TabBlazorTestContext
    {
        private sealed record Row(string Name);

        private IRenderedComponent<Table<Row>> RenderSearchableTable() =>
            Render<Table<Row>>(p => p
                .Add(t => t.Items, new List<Row> { new("a"), new("b") })
                .AddChildContent<Column<Row>>(c => c
                    .Add(x => x.Title, "Name")
                    .Add(x => x.Property, row => row.Name)
                    .Add(x => x.Searchable, true)));

        [Fact]
        public void Lets_header_wrap()
        {
            var cut = RenderSearchableTable();

            var header = cut.Find(".card-header");
            Assert.Contains("flex-wrap", header.ClassList);
            Assert.Contains("gap-2", header.ClassList);
        }

        [Fact]
        public void Gives_search_full_width_below_md()
        {
            var cut = RenderSearchableTable();

            var search = cut.Find(".card-header input").Closest(".ms-auto")!;
            Assert.Contains("col-12", search.ClassList);
            Assert.Contains("col-md-auto", search.ClassList);
        }
    }
}
