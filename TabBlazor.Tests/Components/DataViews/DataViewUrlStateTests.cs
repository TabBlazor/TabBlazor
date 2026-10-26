#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Bunit.TestDoubles;
using Microsoft.Extensions.DependencyInjection;

namespace TabBlazor.Tests.Components.DataViews
{
    public class DataViewUrlStateSerializerTests
    {
        private static readonly DataViewState Defaults = new() { PageSize = 25 };

        [Fact]
        public void Default_state_writes_nothing()
        {
            var query = DataViewUrlState.ToQuery("orders.", Defaults, Defaults, ["status"]);

            Assert.All(query.Values, v => Assert.Null(v));
            Assert.Contains("orders.f.status", query.Keys);
        }

        [Fact]
        public void State_is_written_with_the_prefix()
        {
            var state = new DataViewState
            {
                Page = 3,
                PageSize = 50,
                Search = "odi",
                Sorts = [new SortSpec("orderDate", true)],
                Filters =
                [
                    new FilterSpec("status", FilterOperator.In, new[] { SourceTestStatus.Open, SourceTestStatus.Closed }),
                    new FilterSpec("orderDate", FilterOperator.Between, new DateTime(2026, 1, 1), new DateTime(2026, 3, 31)),
                    new FilterSpec("note", FilterOperator.IsNull),
                    new FilterSpec("total", FilterOperator.GreaterThanOrEqual, 12.5m)
                ]
            };

            var query = DataViewUrlState.ToQuery("orders.", state, Defaults, [], "list", "42");

            Assert.Equal("odi", query["orders.q"]);
            Assert.Equal("-orderDate", query["orders.sort"]);
            Assert.Equal("3", query["orders.page"]);
            Assert.Equal("50", query["orders.size"]);
            Assert.Equal("in:Open,Closed", query["orders.f.status"]);
            Assert.Equal("between:2026-01-01,2026-03-31", query["orders.f.orderDate"]);
            Assert.Equal("isNull", query["orders.f.note"]);
            Assert.Equal("gte:12.5", query["orders.f.total"]);
            Assert.Equal("list", query["orders.view"]);
            Assert.Equal("42", query["orders.current"]);
        }

        [Fact]
        public void State_is_read_back_from_a_url()
        {
            var values = DataViewUrlState.FromUri("orders.",
                "https://x/orders?other=1&orders.q=ada%20love&orders.sort=-orderDate&orders.page=3&orders.size=50" +
                "&orders.f.status=in:Open,Closed&orders.f.total=between:,500&orders.view=list&orders.current=7", Defaults)!;

            Assert.Equal("ada love", values.State.Search);
            Assert.Equal([new SortSpec("orderDate", true)], values.State.Sorts);
            Assert.Equal(3, values.State.Page);
            Assert.Equal(50, values.State.PageSize);
            Assert.Equal(["Open", "Closed"], DataFilters.AsList(values.State.Filters.Single(f => f.Column == "status").Value).Cast<string>());
            var total = values.State.Filters.Single(f => f.Column == "total");
            Assert.Null(total.Value);
            Assert.Equal("500", total.Value2);
            Assert.Equal("list", values.View);
            Assert.Equal("7", values.Current);
        }

        [Fact]
        public void Urls_without_own_keys_give_null()
        {
            Assert.Null(DataViewUrlState.FromUri("orders.", "https://x/orders?q=1&customers.q=2", Defaults));
        }

        [Theory]
        [InlineData("page=0", 1)]
        [InlineData("page=abc", 1)]
        [InlineData("page=7", 7)]
        public void Invalid_values_are_dropped_or_clamped(string query, int expectedPage)
        {
            Assert.Equal(expectedPage, DataViewUrlState.FromUri("", "https://x/?" + query, Defaults)!.State.Page);
        }

        [Fact]
        public void Unknown_operators_are_ignored()
        {
            Assert.Null(DataViewUrlState.ParseFilter("status", "drop:table"));
            Assert.Null(DataViewUrlState.ParseFilter("status", "in:"));
        }

        [Fact]
        public void Ids_must_be_url_safe()
        {
            Assert.Equal("orders.", DataViewUrlState.Prefix("orders"));
            Assert.Equal("", DataViewUrlState.Prefix(null));
            Assert.Throws<InvalidOperationException>(() => DataViewUrlState.Prefix("my orders"));
        }
    }

    public class DataViewUrlStateTests : TabBlazorTestContext
    {
        private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

        private IRenderedComponent<DataView<Row>> RenderUrlView(FakeSource<Row> source, Action<ComponentParameterCollectionBuilder<DataView<Row>>>? configure = null) =>
            Render<DataView<Row>>(p =>
            {
                p.Add(x => x.Source, source)
                    .Add(x => x.Id, "orders")
                    .Add(x => x.UrlState, true)
                    .Add(x => x.PageSize, 10)
                    .Add(x => x.SearchDelay, 0)
                    .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name).Add(x => x.Sortable, true).Add(x => x.Searchable, true))
                    .AddChildContent<DataColumn<Row, decimal>>(c => c.Add(x => x.Value, r => r.Total).Add(x => x.Filterable, true))
                    .AddChildContent<DataColumn<Row, int>>(c => c.Add(x => x.Value, r => r.Id));
                configure?.Invoke(p);
            });

        [Fact]
        public void The_first_request_uses_the_state_in_the_url()
        {
            Navigation.NavigateTo("/orders?orders.q=Row&orders.sort=-name&orders.page=2&orders.size=5&orders.f.total=between:100,");
            var source = FakeSource<Row>.Answering(Row.Many(30));

            RenderUrlView(source);

            var request = source.Calls.Single().Request;
            Assert.Equal(["Row"], request.Search!.Terms);
            Assert.Equal(new SortSpec("name", true), request.Sorts[0]);
            Assert.Equal(PageRequest.Offset(5, 5), request.Page);
            Assert.Equal(new FilterSpec("total", FilterOperator.Between, 100m, null), request.Filters.Single());
        }

        [Fact]
        public void Bound_state_and_filters_learn_the_url_state()
        {
            Navigation.NavigateTo("/orders?orders.f.total=between:100,");
            DataViewState? state = null;
            System.Collections.Generic.IReadOnlyList<FilterSpec>? filters = null;

            RenderUrlView(FakeSource<Row>.Answering(Row.Many(30)), p => p
                .Add(x => x.StateChanged, s => state = s)
                .Add(x => x.FiltersChanged, f => filters = f));

            Assert.Equal("total", state!.Filters.Single().Column);
            Assert.Equal("total", filters!.Single().Column);
        }

        [Fact]
        public void Only_known_columns_and_operators_are_accepted()
        {
            Navigation.NavigateTo("/orders?orders.sort=id&orders.f.id=eq:1&orders.f.name=contains:x&orders.f.total=contains:x");
            var source = FakeSource<Row>.Answering(Row.Many(30));

            RenderUrlView(source);

            var request = source.Calls.Single().Request;
            Assert.DoesNotContain(request.Sorts, s => s.Column == "id");
            Assert.Empty(request.Filters);
        }

        [Fact]
        public void User_changes_are_written_and_other_parameters_kept()
        {
            Navigation.NavigateTo("/orders?tab=2");
            var cut = RenderUrlView(FakeSource<Row>.Answering(Row.Many(30)));

            cut.Find("thead button.dv-sort-button").Click();
            cut.Find(".dv-search input").Input("Row 1");

            Assert.Contains("tab=2", Navigation.Uri);
            Assert.Contains("orders.sort=name", Navigation.Uri);
            Assert.Contains("orders.q=Row%201", Navigation.Uri);
        }

        [Fact]
        public void History_entries_are_replaced_by_default()
        {
            Navigation.NavigateTo("/orders");
            var cut = RenderUrlView(FakeSource<Row>.Answering(Row.Many(30)));

            cut.Find(".dv-pager li:last-child button").Click();

            var entry = ((BunitNavigationManager)Navigation).History.First();
            Assert.Contains("orders.page=2", entry.Uri);
            Assert.True(entry.Options.ReplaceHistoryEntry);
        }

        [Fact]
        public void Push_mode_adds_entries_for_page_changes()
        {
            Navigation.NavigateTo("/orders");
            var cut = RenderUrlView(FakeSource<Row>.Answering(Row.Many(30)), p => p.Add(x => x.UrlHistory, UrlHistoryMode.Push));

            cut.Find(".dv-pager li:last-child button").Click();

            Assert.False(((BunitNavigationManager)Navigation).History.First().Options.ReplaceHistoryEntry);
        }

        [Fact]
        public async Task Back_and_forward_apply_the_url_and_reload()
        {
            Navigation.NavigateTo("/orders");
            var source = FakeSource<Row>.Answering(Row.Many(30));
            var cut = RenderUrlView(source);
            var calls = source.Calls.Count;

            await cut.InvokeAsync(() => Navigation.NavigateTo("/orders?orders.page=3"));

            cut.WaitForAssertion(() => Assert.Equal(calls + 1, source.Calls.Count));
            Assert.Equal(20, source.Last.Request.Page.Skip);
            Assert.Equal(3, cut.Instance.CurrentState.Page);
        }

        [Fact]
        public void Two_views_with_url_state_need_distinct_ids()
        {
            RenderUrlView(FakeSource<Row>.Answering(Row.Many(1)));

            Assert.Throws<InvalidOperationException>(() => RenderUrlView(FakeSource<Row>.Answering(Row.Many(1))));
        }

        [Fact]
        public void The_view_choice_is_kept_in_the_url()
        {
            Navigation.NavigateTo("/orders?orders.view=list");
            var cut = RenderUrlView(FakeSource<Row>.Answering(Row.Many(3)), p => p.Add(x => x.Views, DataViewKind.Table | DataViewKind.List));

            Assert.Equal(DataViewKind.List, cut.Instance.CurrentView);

            cut.Find(".dv-view-switcher button").Click();
            cut.FindAll(".dv-view-switcher .dropdown-item")[0].Click();
            Assert.DoesNotContain("orders.view", Navigation.Uri);
        }

        [Fact]
        public void The_current_item_opens_from_the_url()
        {
            Navigation.NavigateTo("/orders?orders.current=2");
            var cut = RenderUrlView(FakeSource<Row>.Answering(Row.Many(3)), p => p
                .Add(x => x.KeySelector, r => r.Id)
                .Add(x => x.DetailsMode, DataDetailsMode.Drawer)
                .AddChildContent<DetailsTemplate<Row>>(t => t.Add(x => x.ChildContent, ctx => b => b.AddContent(0, $"details {ctx.Item.Id}"))));

            Assert.Contains("details 2", cut.Find(".offcanvas").TextContent);

            cut.FindAll("tbody tr")[2].Click();
            Assert.Contains("orders.current=3", Navigation.Uri);
        }

        [Fact]
        public void Without_a_key_the_current_item_stays_out_of_the_url()
        {
            Navigation.NavigateTo("/orders");
            var cut = RenderUrlView(FakeSource<Row>.Answering(Row.Many(3)), p => p
                .Add(x => x.DetailsMode, DataDetailsMode.Drawer)
                .AddChildContent<DetailsTemplate<Row>>(t => t.Add(x => x.ChildContent, ctx => b => b.AddContent(0, $"details {ctx.Item.Id}"))));

            cut.FindAll("tbody tr")[1].Click();

            Assert.Contains("details 2", cut.Find(".offcanvas").TextContent);
            Assert.DoesNotContain("orders.current", Navigation.Uri);
        }

        [Fact]
        public void A_cleared_default_search_and_filter_are_written_and_read_back()
        {
            var defaults = new DataViewState
            {
                Search = "row",
                Filters = [new FilterSpec("total", FilterOperator.GreaterThanOrEqual, 10m)]
            };

            var query = DataViewUrlState.ToQuery("orders.", new DataViewState { Page = 2 }, defaults, ["total"]);
            Assert.Equal("", query["orders.q"]);
            Assert.Equal("none", query["orders.f"]);
            Assert.Null(query["orders.f.total"]);

            var values = DataViewUrlState.FromUri("orders.", "https://x/?orders.q=&orders.f=none&orders.page=2", defaults)!;
            Assert.True(string.IsNullOrEmpty(values.State.Search));
            Assert.Empty(values.State.Filters);
        }

        [Fact]
        public async Task Clearing_a_default_search_survives_a_reload()
        {
            Navigation.NavigateTo("/orders");
            var first = RenderUrlView(FakeSource<Row>.Answering(Row.Many(30)), p => p.Add(x => x.State, new DataViewState { Search = "Row 1" }));

            await first.InvokeAsync(() => first.Instance.SetSearchAsync(null));
            Assert.Contains("orders.q=", Navigation.Uri);
            await first.InvokeAsync(() => first.Instance.DisposeAsync().AsTask());

            var source = FakeSource<Row>.Answering(Row.Many(30));
            RenderUrlView(source, p => p.Add(x => x.State, new DataViewState { Search = "Row 1" }));
            Assert.Null(source.Calls[0].Request.Search);
        }

        [Fact]
        public void Missing_search_and_filter_keys_keep_the_defaults()
        {
            var defaults = new DataViewState
            {
                Search = "row",
                Filters = [new FilterSpec("total", FilterOperator.GreaterThanOrEqual, 10m)]
            };

            Assert.All(DataViewUrlState.ToQuery("orders.", defaults, defaults, ["total"]).Values, v => Assert.Null(v));

            var values = DataViewUrlState.FromUri("orders.", "https://x/?orders.page=2", defaults)!;
            Assert.Equal("row", values.State.Search);
            Assert.Equal("total", values.State.Filters.Single().Column);
        }

        [Fact]
        public async Task Back_and_forward_report_the_filters()
        {
            Navigation.NavigateTo("/orders");
            IReadOnlyList<FilterSpec>? filters = null;
            var cut = RenderUrlView(FakeSource<Row>.Answering(Row.Many(30)), p => p.Add(x => x.FiltersChanged, f => filters = f));

            await cut.InvokeAsync(() => Navigation.NavigateTo("/orders?orders.f.total=between:100,"));

            cut.WaitForAssertion(() => Assert.Equal("total", filters?.Single().Column));
        }

        [Fact]
        public void Opening_details_while_editing_does_not_ask_to_discard()
        {
            Navigation.NavigateTo("/orders");
            var confirmations = new List<string>();
            var cut = RenderUrlView(FakeSource<Row>.Answering(Row.Many(3)), p => p
                .Add(x => x.KeySelector, r => r.Id)
                .Add(x => x.DetailsMode, DataDetailsMode.Drawer)
                .Add(x => x.EditMode, DataEditMode.Inline)
                .Add(x => x.SaveItem, ctx => Task.FromResult(EditResult.Saved(ctx.Draft)))
                .Add(x => x.OnConfirm, text =>
                {
                    confirmations.Add(text);
                    return Task.FromResult(false);
                })
                .AddChildContent<DetailsTemplate<Row>>(t => t.Add(x => x.ChildContent, ctx => b => b.AddContent(0, $"details {ctx.Item.Id}"))));
            cut.Find("tbody tr button.dv-action").Click();
            cut.Find("tr.dv-edit-row input.form-control").Change("Changed");

            cut.FindAll("tbody tr")[1].Click();

            Assert.Contains("orders.current=2", Navigation.Uri);
            Assert.Empty(confirmations);
        }

        [Fact]
        public async Task A_view_being_disposed_frees_its_url_prefix_before_its_js_cleanup_ends()
        {
            JSInterop.Setup<bool>("tabBlazor.watchMedia", _ => true).SetResult(false);
            var unwatch = JSInterop.SetupVoid("tabBlazor.unwatchMedia");
            var first = RenderUrlView(FakeSource<Row>.Answering(Row.Many(1)));

            var disposing = first.InvokeAsync(() => first.Instance.DisposeAsync().AsTask());
            var second = RenderUrlView(FakeSource<Row>.Answering(Row.Many(1)));
            unwatch.SetVoidResult();
            await disposing;

            Assert.NotNull(second.Instance);
        }
    }
}
