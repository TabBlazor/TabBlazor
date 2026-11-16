#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace TabBlazor.Tests.Components.DataViews
{
    public class DataViewExportTests : TabBlazorTestContext
    {
        private DataExportContext<Row>? captured;
        private readonly List<Row> exported = [];
        private Exception? callbackError;

        private async Task CollectAsync(DataExportContext<Row> context)
        {
            captured = context;
            try
            {
                await foreach (var row in context.GetItemsAsync())
                {
                    exported.Add(row);
                }
            }
            catch (Exception ex)
            {
                callbackError = ex;
            }
        }

        private IRenderedComponent<DataView<Row>> RenderExportable(
            Action<ComponentParameterCollectionBuilder<DataView<Row>>>? configure = null)
        {
            return Render<DataView<Row>>(p =>
            {
                p.Add(x => x.OnExport, CollectAsync)
                    .Add(x => x.KeySelector, r => r.Id)
                    .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name).Add(x => x.Searchable, true))
                    .AddChildContent<DataColumn<Row, decimal>>(c => c.Add(x => x.Value, r => r.Total).Add(x => x.Format, "N2"));
                configure?.Invoke(p);
            });
        }

        private static Task ExportAsync(IRenderedComponent<DataView<Row>> cut, DataExportScope scope) =>
            cut.InvokeAsync(() => cut.Instance.ExportAsync(scope));

        [Fact]
        public async Task Exports_the_rows_of_the_current_page()
        {
            var cut = RenderExportable(p => p.Add(x => x.Items, Row.Many(10)).Add(x => x.PageSize, 3));

            await ExportAsync(cut, DataExportScope.CurrentPage);

            Assert.Equal([1, 2, 3], exported.Select(r => r.Id));
            Assert.Equal(DataExportScope.CurrentPage, captured!.Scope);
        }

        [Fact]
        public async Task Exports_every_matching_row_by_paging_the_source()
        {
            // A source that never returns more than 4 rows at a time, whatever the page size asks for.
            var all = Row.Many(10);
            var calls = 0;
            var source = new FakeSource<Row>
            {
                Respond = request =>
                {
                    calls++;
                    return new DataPage<Row>(all.Skip(request.Page.Skip).Take(Math.Min(4, request.Page.Take)).ToList(), all.Count);
                }
            };

            var cut = RenderExportable(p => p.Add(x => x.Source, source).Add(x => x.PageSize, 2));
            var callsBefore = calls;

            await ExportAsync(cut, DataExportScope.AllMatching);

            Assert.Equal(Enumerable.Range(1, 10), exported.Select(r => r.Id));
            Assert.True(calls - callsBefore >= 3, "every matching row is read a page at a time");
        }

        [Fact]
        public async Task Exports_all_matching_rows_regardless_of_the_page_on_screen()
        {
            var cut = RenderExportable(p => p.Add(x => x.Items, Row.Many(10)).Add(x => x.PageSize, 3));
            await cut.InvokeAsync(() => cut.Instance.GoToPageAsync(3));

            await ExportAsync(cut, DataExportScope.AllMatching);

            Assert.Equal(Enumerable.Range(1, 10), exported.Select(r => r.Id));
        }

        [Fact]
        public async Task Exports_only_the_rows_matching_the_search()
        {
            var cut = RenderExportable(p => p.Add(x => x.Items, Row.Many(12)).Add(x => x.PageSize, 5));

            await cut.InvokeAsync(() => cut.Instance.SetSearchAsync("Row 1"));
            await ExportAsync(cut, DataExportScope.AllMatching);

            Assert.Equal([1, 10, 11, 12], exported.Select(r => r.Id));
            Assert.Equal("Row 1", captured!.Request.Search?.Text);
        }

        [Fact]
        public async Task Stops_at_MaxExportRows_instead_of_writing_a_short_file()
        {
            var cut = RenderExportable(p => p.Add(x => x.Items, Row.Many(10))
                .Add(x => x.PageSize, 2)
                .Add(x => x.MaxExportRows, 4));

            await ExportAsync(cut, DataExportScope.AllMatching);

            Assert.IsType<InvalidOperationException>(callbackError);
            Assert.Contains("4", callbackError!.Message);
        }

        [Fact]
        public async Task Exports_up_to_the_limit_without_complaining()
        {
            var cut = RenderExportable(p => p.Add(x => x.Items, Row.Many(4))
                .Add(x => x.PageSize, 2)
                .Add(x => x.MaxExportRows, 4));

            await ExportAsync(cut, DataExportScope.AllMatching);

            Assert.Null(callbackError);
            Assert.Equal(4, exported.Count);
        }

        [Fact]
        public async Task Exports_the_selected_rows_without_asking_the_source()
        {
            var source = FakeSource<Row>.Answering(Row.Many(6));
            var cut = RenderExportable(p => p.Add(x => x.Source, source)
                .Add(x => x.SelectionMode, DataSelectionMode.Multiple));

            cut.FindAll("tbody tr")[1].QuerySelector("input.dv-select")!.Change(true);
            var callsBefore = source.Calls.Count;

            await ExportAsync(cut, DataExportScope.Selected);

            Assert.Equal([2], exported.Select(r => r.Id));
            Assert.Equal([2], captured!.SelectedKeys!);
            Assert.Equal(callsBefore, source.Calls.Count);
        }

        [Fact]
        public async Task Leaves_out_columns_that_are_not_exportable()
        {
            var cut = RenderExportable(p => p.Add(x => x.Items, Row.Many(1))
                .AddChildContent<DataColumn<Row, bool>>(c => c.Add(x => x.Value, r => r.Active).Add(x => x.Exportable, false)));

            await ExportAsync(cut, DataExportScope.CurrentPage);

            Assert.Equal(["name", "total"], captured!.Columns.Select(c => c.Id));
        }

        [Fact]
        public async Task Exports_the_text_the_cell_shows()
        {
            var cut = RenderExportable(p => p.Add(x => x.Items, Row.Many(1)));

            await ExportAsync(cut, DataExportScope.CurrentPage);

            var total = captured!.Columns.Single(c => c.Id == "total");
            Assert.Equal(10.5m.ToString("N2"), total.GetExportText(exported[0]));
            Assert.Equal(10.5m, total.GetExportValue(exported[0]));
        }

        [Fact]
        public async Task Uses_ExportValue_for_a_column_that_has_no_value()
        {
            var cut = RenderExportable(p => p.Add(x => x.Items, Row.Many(1))
                .AddChildContent<DataColumn<Row, string>>(c => c
                    .Add(x => x.Title, "Status")
                    .Add(x => x.ExportValue, r => (object?)r.Status)
                    .Add(x => x.Template, (RenderFragment<Row>)(row => builder => builder.AddContent(0, "badge")))));

            await ExportAsync(cut, DataExportScope.CurrentPage);

            var status = captured!.Columns.Single(c => c.Title == "Status");
            Assert.Equal("On hold", status.GetExportText(exported[0]));
        }

        [Fact]
        public async Task Exports_an_empty_cell_for_a_template_column_with_no_export_value()
        {
            var cut = RenderExportable(p => p.Add(x => x.Items, Row.Many(1))
                .AddChildContent<DataColumn<Row, string>>(c => c
                    .Add(x => x.Title, "Actions")
                    .Add(x => x.Template, (RenderFragment<Row>)(row => builder => builder.AddContent(0, "edit")))));

            await ExportAsync(cut, DataExportScope.CurrentPage);

            var actions = captured!.Columns.Single(c => c.Title == "Actions");
            Assert.Null(actions.GetExportText(exported[0]));
        }

        [Fact]
        public void Export_is_offered_in_the_menu_only_with_a_callback()
        {
            var without = Render<DataView<Row>>(p => p.Add(x => x.Items, Row.Many(2))
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name)));
            without.Find(".dv-menu").Click();
            Assert.Empty(without.FindAll(".dv-menu-export"));

            var with = RenderExportable(p => p.Add(x => x.Items, Row.Many(2)));
            with.Find(".dv-menu").Click();
            Assert.NotEmpty(with.FindAll(".dv-menu-export"));
        }

        [Fact]
        public void Export_is_offered_in_the_selection_bar_while_rows_are_selected()
        {
            var cut = RenderExportable(p => p.Add(x => x.Items, Row.Many(3))
                .Add(x => x.SelectionMode, DataSelectionMode.Multiple));

            Assert.Empty(cut.FindAll(".dv-bulk-export"));

            cut.FindAll("tbody tr")[0].QuerySelector("input.dv-select")!.Change(true);

            Assert.NotEmpty(cut.FindAll(".dv-bulk-export"));
        }

        [Fact]
        public async Task Reports_a_failing_callback_instead_of_throwing()
        {
            var cut = Render<DataView<Row>>(p => p.Add(x => x.Items, Row.Many(2))
                .Add(x => x.OnExport, _ => throw new InvalidOperationException("no disk"))
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name)));

            await ExportAsync(cut, DataExportScope.CurrentPage);

            Assert.False(cut.Instance.IsExporting);
            Assert.Contains("no disk", cut.Markup);
        }

        [Fact]
        public async Task Cancelling_ends_the_export_quietly()
        {
            var all = Row.Many(12);
            var source = new FakeSource<Row>
            {
                Respond = request => new DataPage<Row>(
                    all.Skip(request.Page.Skip).Take(Math.Min(4, request.Page.Take)).ToList(), all.Count)
            };

            DataView<Row>? view = null;
            var rendered = Render<DataView<Row>>(p => p
                .Add(x => x.Source, source)
                .Add(x => x.OnExport, async context =>
                {
                    await foreach (var row in context.GetItemsAsync())
                    {
                        exported.Add(row);
                        view!.CancelExport();
                    }
                })
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name)));
            view = rendered.Instance;

            await ExportAsync(rendered, DataExportScope.AllMatching);

            // The page in flight finishes, the next one is never asked for, and nothing is reported as a failure.
            Assert.Equal(4, exported.Count);
            Assert.False(rendered.Instance.IsExporting);
            Assert.DoesNotContain("failed", rendered.Markup, StringComparison.OrdinalIgnoreCase);
        }
    }
}
