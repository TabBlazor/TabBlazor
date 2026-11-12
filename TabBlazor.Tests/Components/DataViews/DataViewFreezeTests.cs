#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp.Dom;

namespace TabBlazor.Tests.Components.DataViews
{
    public class DataViewFreezeTests : TabBlazorTestContext
    {
        private IRenderedComponent<DataView<Row>> RenderView(
            ColumnFreeze name = ColumnFreeze.None,
            ColumnFreeze total = ColumnFreeze.None,
            ColumnFreeze active = ColumnFreeze.None,
            Action<ComponentParameterCollectionBuilder<DataView<Row>>>? configure = null) =>
            Render<DataView<Row>>(p =>
            {
                p.Add(x => x.Items, Row.Many(3))
                    .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name).Add(x => x.Freeze, name))
                    .AddChildContent<DataColumn<Row, decimal>>(c => c.Add(x => x.Value, r => r.Total).Add(x => x.Freeze, total))
                    .AddChildContent<DataColumn<Row, bool>>(c => c.Add(x => x.Value, r => r.Active).Add(x => x.Freeze, active));
                configure?.Invoke(p);
            });

        private static void WithActions(ComponentParameterCollectionBuilder<DataView<Row>> p) =>
            p.AddChildContent<RowActions<Row>>(a => a.AddChildContent<RowAction<Row>>(x => x.Add(y => y.Title, "Go")));

        private static string[] HeaderTitles(IRenderedComponent<DataView<Row>> cut) =>
            cut.FindAll("thead tr:first-child th[data-column-id]").Select(th => th.GetAttribute("data-column-id")!).ToArray();

        private static IElement[] FirstRowCells(IRenderedComponent<DataView<Row>> cut) =>
            cut.Find("tbody tr").QuerySelectorAll("td").ToArray();

        [Fact]
        public void Columns_without_freeze_render_no_sticky_cells()
        {
            var cut = RenderView();

            Assert.Empty(cut.FindAll(".dv-fs, .dv-fe"));
            Assert.Null(cut.Find("tbody td").GetAttribute("style"));
        }

        [Fact]
        public void Start_frozen_columns_move_first_and_end_frozen_columns_last()
        {
            var cut = RenderView(name: ColumnFreeze.End, active: ColumnFreeze.Start);

            Assert.Equal(["active", "total", "name"], HeaderTitles(cut));
        }

        [Fact]
        public void Frozen_cells_get_sticky_classes_and_offsets_by_position()
        {
            var cut = RenderView(name: ColumnFreeze.Start, total: ColumnFreeze.Start, active: ColumnFreeze.End);
            var cells = FirstRowCells(cut);

            Assert.Contains("dv-fs", cells[0].ClassList);
            Assert.DoesNotContain("dv-fs-edge", cells[0].ClassList);
            Assert.Contains("--dv-inset: var(--dv-fs-0, 0px)", cells[0].GetAttribute("style"));

            Assert.Contains("dv-fs-edge", cells[1].ClassList);
            Assert.Contains("--dv-inset: var(--dv-fs-1, 0px)", cells[1].GetAttribute("style"));

            Assert.Contains("dv-fe", cells[2].ClassList);
            Assert.Contains("dv-fe-edge", cells[2].ClassList);
            Assert.Contains("--dv-inset: var(--dv-fe-0, 0px)", cells[2].GetAttribute("style"));
        }

        [Fact]
        public void Header_and_body_cells_get_the_same_freeze()
        {
            var cut = RenderView(total: ColumnFreeze.End);

            var header = cut.Find("th[data-column-id=total]");
            Assert.Contains("dv-fe", header.ClassList);
            Assert.Contains("--dv-inset: var(--dv-fe-0, 0px)", header.GetAttribute("style"));
        }

        [Fact]
        public void Freeze_style_is_added_to_the_column_style()
        {
            var cut = RenderView(name: ColumnFreeze.Start, configure: p => p.AddChildContent<DataColumn<Row, int>>(c => c
                .Add(x => x.Value, r => r.Id)
                .Add(x => x.Freeze, ColumnFreeze.Start)
                .Add(x => x.CellStyle, _ => "color: red;")));

            var style = FirstRowCells(cut)[1].GetAttribute("style");
            Assert.Equal("color: red;--dv-inset: var(--dv-fs-1, 0px)", style);
        }

        [Fact]
        public void Row_actions_freeze_to_the_end_by_default()
        {
            var cut = RenderView(configure: WithActions);

            var actions = cut.Find("tbody td.dv-actions-cell");
            Assert.Contains("dv-fe", actions.ClassList);
            Assert.Contains("dv-fe-edge", actions.ClassList);
            Assert.Contains("dv-fe", cut.Find("thead th.dv-actions-column").ClassList);
        }

        [Fact]
        public void Row_actions_do_not_freeze_when_turned_off()
        {
            var cut = RenderView(configure: p =>
            {
                WithActions(p);
                p.Add(x => x.FreezeRowActions, false);
            });

            Assert.Empty(cut.FindAll(".dv-fs, .dv-fe"));
        }

        [Fact]
        public void Row_actions_after_an_end_frozen_column_freeze_with_it()
        {
            var cut = RenderView(total: ColumnFreeze.End, configure: p =>
            {
                WithActions(p);
                p.Add(x => x.FreezeRowActions, false);
            });

            var cells = FirstRowCells(cut);
            Assert.Contains("--dv-inset: var(--dv-fe-1, 0px)", cells[2].GetAttribute("style"));
            Assert.Contains("dv-fe-edge", cells[2].ClassList);
            Assert.Contains("--dv-inset: var(--dv-fe-0, 0px)", cells[3].GetAttribute("style"));
            Assert.DoesNotContain("dv-fe-edge", cells[3].ClassList);
        }

        [Fact]
        public void Select_column_freezes_before_start_frozen_columns()
        {
            var cut = RenderView(name: ColumnFreeze.Start, configure: p => p.Add(x => x.SelectionMode, DataSelectionMode.Multiple));

            var cells = FirstRowCells(cut);
            Assert.Contains("dv-select-cell", cells[0].ClassList);
            Assert.Contains("--dv-inset: var(--dv-fs-0, 0px)", cells[0].GetAttribute("style"));
            Assert.Contains("--dv-inset: var(--dv-fs-1, 0px)", cells[1].GetAttribute("style"));
            Assert.Contains("dv-fs", cut.Find("thead th.dv-select-column").ClassList);
        }

        [Fact]
        public void Select_column_scrolls_when_nothing_is_frozen_at_the_start()
        {
            var cut = RenderView(configure: p => p.Add(x => x.SelectionMode, DataSelectionMode.Multiple));

            Assert.DoesNotContain("dv-fs", cut.Find("tbody td.dv-select-cell").ClassList);
        }

        [Fact]
        public void Row_link_sits_in_the_first_scrolling_cell()
        {
            var cut = RenderView(name: ColumnFreeze.Start, configure: p => p.Add(x => x.RowHref, r => $"rows/{r.Id}"));

            var cells = FirstRowCells(cut);
            Assert.Null(cells[0].QuerySelector("a.dv-row-anchor"));
            Assert.NotNull(cells[1].QuerySelector("a.dv-row-anchor"));
        }

        [Fact]
        public void Small_screen_scroll_layout_freezes_the_first_column()
        {
            JSInterop.Setup<bool>("tabBlazor.watchMedia", _ => true).SetResult(true);

            var cut = RenderView(configure: p => p.Add(x => x.MobileLayout, DataMobileLayout.Scroll));

            cut.WaitForAssertion(() => Assert.Contains("dv-fs", FirstRowCells(cut)[0].ClassList));
        }

        [Fact]
        public void Small_screen_stacked_layout_freezes_nothing()
        {
            JSInterop.Setup<bool>("tabBlazor.watchMedia", _ => true).SetResult(true);

            var cut = RenderView(name: ColumnFreeze.Start, configure: WithActions);

            cut.WaitForAssertion(() => Assert.Contains("dv-small", cut.Find(".dataview").ClassList));
            Assert.Empty(cut.FindAll(".dv-fs, .dv-fe"));
        }

        [Fact]
        public void Frozen_cells_are_measured_after_render()
        {
            RenderView(name: ColumnFreeze.Start);

            var calls = JSInterop.Invocations["tabBlazor.syncFrozenColumns"];
            Assert.NotEmpty(calls);
            Assert.All(calls, call => Assert.Equal(true, call.Arguments[1]));
        }

        [Fact]
        public void Views_without_frozen_cells_do_not_call_js()
        {
            RenderView();

            JSInterop.VerifyNotInvoke("tabBlazor.syncFrozenColumns");
        }

        [Fact]
        public void Inline_edit_row_keeps_the_freeze()
        {
            var cut = RenderView(name: ColumnFreeze.Start, configure: p => p
                .Add(x => x.SaveItem, ctx => Task.FromResult(EditResult.Saved(ctx.Draft)))
                .Add(x => x.EditMode, DataEditMode.Inline));

            cut.InvokeAsync(() => cut.Instance.EditAsync(cut.Instance.Engine.Items[0]));

            var editCells = cut.Find("tr.dv-edit-row").QuerySelectorAll("td");
            Assert.Contains("dv-fs", editCells[0].ClassList);
        }
    }
}
