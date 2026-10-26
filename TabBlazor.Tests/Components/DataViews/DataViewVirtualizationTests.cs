#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace TabBlazor.Tests.Components.DataViews
{
    public class DataViewVirtualizationTests : TabBlazorTestContext
    {
        private IRenderedComponent<DataView<Row>> RenderVirtual(Action<ComponentParameterCollectionBuilder<DataView<Row>>> configure, int pageSize = 25) =>
            Render<DataView<Row>>(p =>
            {
                p.Add(x => x.Virtualize, true)
                    .Add(x => x.Height, "400px")
                    .Add(x => x.PageSize, pageSize)
                    .Add(x => x.KeySelector, r => r.Id)
                    .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name).Add(x => x.Sortable, true));
                configure(p);
            });

        private static Task<ItemsProviderResult<Row>> Provide(IRenderedComponent<DataView<Row>> cut, int start, int count) =>
            cut.InvokeAsync(() => cut.Instance.ProvideVirtualItemsAsync(new ItemsProviderRequest(start, count, CancellationToken.None)).AsTask());

        [Fact]
        public void Renders_a_virtualized_table_with_a_row_count_instead_of_a_pager()
        {
            var cut = RenderVirtual(p => p.Add(x => x.Items, Row.Many(1000)));

            var table = cut.Find("table");
            Assert.Contains("dv-virtual", table.ClassList);
            Assert.Contains("dv-sticky-header", table.ClassList);
            Assert.Contains("--dv-row-height: 44px", table.GetAttribute("style"));
            Assert.Contains("height: 400px", cut.Find(".dv-scroll").GetAttribute("style"));
            Assert.Contains(1000.ToString("N0"), cut.Find(".dv-row-count").TextContent);
            Assert.Empty(cut.FindAll(".dv-pager"));
        }

        [Fact]
        public async Task Offset_sources_fetch_the_visible_range_and_cache_it()
        {
            var source = FakeSource<Row>.Answering(Row.Many(1000));
            var cut = RenderVirtual(p => p.Add(x => x.Source, source));

            var result = await Provide(cut, 500, 20);
            var calls = source.Calls.Count;

            Assert.Equal(1000, result.TotalItemCount);
            Assert.Equal(Enumerable.Range(501, 20), result.Items.Select(r => r.Id));
            Assert.Equal(PageRequest.Offset(500, 25), source.Last.Request.Page);

            await Provide(cut, 505, 15);
            Assert.Equal(calls, source.Calls.Count);
        }

        [Fact]
        public async Task The_first_page_is_served_from_the_initial_load()
        {
            var source = FakeSource<Row>.Answering(Row.Many(1000));
            var cut = RenderVirtual(p => p.Add(x => x.Source, source));
            var calls = source.Calls.Count;

            var result = await Provide(cut, 0, 15);

            Assert.Equal(15, result.Items.Count());
            Assert.Equal(calls, source.Calls.Count);
        }

        [Fact]
        public async Task Cursor_sources_scroll_infinitely()
        {
            var memory = new InMemorySource<Row>(Row.Many(60), new InMemorySourceOptions { PagingMode = PagingMode.Cursor, IncludeTotalCount = false });
            var source = new FakeSource<Row> { PagingMode = PagingMode.Cursor, Respond = memory.GetPage };
            var cut = RenderVirtual(p => p.Add(x => x.Source, source), pageSize: 20);

            var result = await Provide(cut, 30, 15);

            Assert.Equal(Enumerable.Range(31, 15), result.Items.Select(r => r.Id));
            Assert.Equal(60, result.TotalItemCount);
            Assert.Equal(PageRequest.Cursor("40", 20), source.Last.Request.Page);

            result = await Provide(cut, 50, 20);
            Assert.Equal(60, result.TotalItemCount);
            Assert.Equal(10, result.Items.Count());
        }

        [Fact]
        public async Task Changing_the_sort_starts_over()
        {
            var source = FakeSource<Row>.Answering(Row.Many(1000));
            var cut = RenderVirtual(p => p.Add(x => x.Source, source));
            await Provide(cut, 100, 20);

            await cut.InvokeAsync(() => cut.Instance.SetSortAsync("name", descending: true));
            var calls = source.Calls.Count;
            var result = await Provide(cut, 100, 20);

            Assert.Equal(calls + 1, source.Calls.Count);
            Assert.True(source.Last.Request.Sorts[0].Descending);
            var expected = Row.Many(1000).OrderByDescending(r => r.Name, StringComparer.CurrentCulture).ElementAt(100).Id;
            Assert.Equal(expected, result.Items.First().Id);
        }

        [Fact]
        public async Task Select_all_means_all_matching_while_virtualized()
        {
            DataSelection<Row>? selection = null;
            var cut = RenderVirtual(p => p
                .Add(x => x.Items, Row.Many(1000))
                .Add(x => x.SelectionMode, DataSelectionMode.Multiple)
                .Add(x => x.OnSelectionChanged, s => selection = s));

            await cut.InvokeAsync(() => cut.Instance.TogglePageSelectionAsync());

            Assert.True(selection!.AllMatching);
            Assert.Equal(1000, selection.Count);
        }

        [Fact]
        public void Edits_in_a_popup_and_shows_details_in_a_drawer_by_default()
        {
            var cut = RenderVirtual(p => p
                .Add(x => x.Items, Row.Many(100))
                .Add(x => x.SaveItem, ctx => Task.FromResult(EditResult.Saved(ctx.Draft)))
                .AddChildContent<DetailsTemplate<Row>>(t => t.Add(x => x.ChildContent, ctx => b => b.AddContent(0, ctx.Item.Name))));

            Assert.Equal(DataEditMode.Popup, cut.Instance.EffectiveEditMode);
            Assert.Equal(DataDetailsMode.Drawer, cut.Instance.EffectiveDetailsMode);
            Assert.Empty(cut.FindAll("button.dv-expand"));
        }

        [Fact]
        public void Renders_the_list_view_virtualized()
        {
            var cut = RenderVirtual(p => p.Add(x => x.Items, Row.Many(100)).Add(x => x.View, DataViewKind.List));

            Assert.Contains("height: 400px", cut.Find(".dv-list").GetAttribute("style"));
            Assert.NotNull(cut.FindComponent<Virtualize<Row>>());
        }

        [Fact]
        public async Task A_new_load_waits_for_a_scroll_fetch_and_drops_its_rows()
        {
            var rows = Row.Many(100);
            var source = new FakeSource<Row>();
            var cut = RenderVirtual(p => p.Add(x => x.Source, source));
            await source.WaitForCallsAsync(1);
            await cut.InvokeAsync(() => source.Calls[0].Response.SetResult(new DataPage<Row>(rows.Take(25).ToList(), null)));
            cut.WaitForAssertion(() => Assert.Equal(25, cut.Instance.VirtualRowCount));

            var provide = Provide(cut, 0, 40);
            await source.WaitForCallsAsync(2);
            var fetch = source.Calls[1];
            Assert.Equal(PageRequest.Offset(25, 25), fetch.Request.Page);
            var sort = cut.InvokeAsync(() => cut.Instance.SetSortAsync("name", descending: true));

            cut.WaitForAssertion(() => Assert.True(fetch.Token.IsCancellationRequested));
            Assert.Equal(2, source.Calls.Count);

            fetch.Response.SetResult(new DataPage<Row>(rows.Skip(25).Take(25).ToList(), null));
            await source.WaitForCallsAsync(3);
            Assert.Equal(25, cut.Instance.VirtualRowCount);

            source.Respond = r => new DataPage<Row>(rows.Skip(r.Page.Skip).Take(r.Page.Take).ToList(), null);
            source.Calls[2].Response.SetResult(new DataPage<Row>(rows.Take(25).ToList(), null));
            await sort;
            await provide;
        }
    }
}
