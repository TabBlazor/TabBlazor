#nullable enable
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace TabBlazor.Tests.Components.DataViews
{
    public class DataViewColumnsTests : TabBlazorTestContext
    {
        [Fact]
        public async Task Resizable_columns_get_drag_handles_and_keep_the_new_width()
        {
            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Items, Row.Many(2))
                .Add(x => x.ResizableColumns, true)
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name)));

            Assert.NotNull(cut.Find("thead th .dv-resize-handle"));
            Assert.Equal("name", cut.Find("thead th").GetAttribute("data-column-id"));
            JSInterop.VerifyInvoke("tabBlazor.attachColumnResize");

            await cut.InvokeAsync(() => cut.Instance.OnColumnResized("name", 180));

            Assert.Equal("width: 180px;min-width: 180px;", cut.Find("thead th").GetAttribute("style"));
            Assert.Equal(180, cut.Instance.ColumnWidths["name"]);
        }

        [Fact]
        public void Columns_are_not_resizable_by_default()
        {
            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Items, Row.Many(1))
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name)));

            Assert.Empty(cut.FindAll(".dv-resize-handle"));
            JSInterop.VerifyNotInvoke("tabBlazor.attachColumnResize");
        }

        [Fact]
        public void Column_options_open_from_the_header()
        {
            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Items, Row.Many(1))
                .AddChildContent<DataColumn<Row, string>>(c => c
                    .Add(x => x.Value, r => r.Name)
                    .Add(x => x.ColumnOptions, b => b.AddMarkupContent(0, "<span class=\"my-option\">Only mine</span>"))));

            Assert.Empty(cut.FindAll(".my-option"));
            cut.Find("thead .dv-column-options button").Click();

            Assert.Equal("Only mine", cut.Find("thead .my-option").TextContent);
        }

        [Fact]
        public async Task A_column_changed_at_runtime_is_used_by_the_next_request()
        {
            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Items, Row.Many(3))
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name).Add(x => x.Sortable, true)));

            cut.Render(p => p.AddChildContent<DataColumn<Row, string>>(c => c
                .Add(x => x.Value, r => r.Name).Add(x => x.Sortable, true).Add(x => x.Id, "title")));
            await cut.InvokeAsync(() => cut.Instance.SetSortAsync("title", descending: true));

            Assert.Null(cut.Instance.Error);
            Assert.Equal("Row 03", cut.Find("tbody td").TextContent.Trim());
        }

        [Fact]
        public async Task Resizing_follows_the_table_when_it_is_rebuilt()
        {
            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Items, Row.Many(2))
                .Add(x => x.ResizableColumns, true)
                .Add(x => x.DetailsMode, DataDetailsMode.SidePanel)
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name))
                .AddChildContent<DetailsTemplate<Row>>(t => t.Add(x => x.ChildContent, ctx => b => b.AddContent(0, ctx.Item.Name))));
            JSInterop.VerifyInvoke("tabBlazor.attachColumnResize", 1);

            // Below the breakpoint the side panel becomes a drawer, which rebuilds the table.
            await cut.InvokeAsync(() => cut.Instance.OnMediaChanged(true));

            JSInterop.VerifyInvoke("tabBlazor.attachColumnResize", 2);
        }
    }
}
