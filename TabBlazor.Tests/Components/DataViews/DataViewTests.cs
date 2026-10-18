#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace TabBlazor.Tests.Components.DataViews
{
    public class DataViewTests : TabBlazorTestContext
    {
        private readonly FakeTimeProvider time = new();

        public DataViewTests()
        {
            Services.AddSingleton<TimeProvider>(time);
        }

        private IRenderedComponent<DataView<Row>> RenderView(
            Action<ComponentParameterCollectionBuilder<DataView<Row>>> configure,
            bool withDefaultColumns = true)
        {
            return Render<DataView<Row>>(p =>
            {
                configure(p);
                if (withDefaultColumns)
                {
                    p.AddChildContent<DataColumn<Row, int>>(c => c.Add(x => x.Value, r => r.Id).Add(x => x.Sortable, true));
                    p.AddChildContent<DataColumn<Row, string>>(c => c
                        .Add(x => x.Value, r => r.Name).Add(x => x.Sortable, true).Add(x => x.Searchable, true));
                }
            });
        }

        private static List<string> CellTexts(IRenderedComponent<DataView<Row>> cut, int column) =>
            cut.FindAll("tbody tr").Select(r => r.QuerySelectorAll("td")[column].TextContent.Trim()).ToList();

        // Rendering

        [Fact]
        public void Renders_a_row_per_item_and_a_header_per_column()
        {
            var cut = RenderView(p => p.Add(x => x.Items, Row.Many(3)));

            Assert.Equal(["Id", "Name"], cut.FindAll("thead th").Select(th => th.TextContent.Trim()));
            Assert.Equal(["Row 01", "Row 02", "Row 03"], CellTexts(cut, 1));
        }

        [Fact]
        public void Renders_card_with_id_class_and_title()
        {
            var cut = RenderView(p => p
                .Add(x => x.Items, Row.Many(1))
                .Add(x => x.Id, "orders")
                .Add(x => x.Class, "my-card")
                .Add(x => x.Title, "Orders"));

            var card = cut.Find("div.card.dataview");
            Assert.Equal("orders", card.Id);
            Assert.Contains("my-card", card.ClassList);
            Assert.Equal("Orders", cut.Find(".card-header .card-title").TextContent);
        }

        [Fact]
        public void Renders_header_and_toolbar_templates()
        {
            var cut = RenderView(p => p
                .Add(x => x.Items, Row.Many(1))
                .AddChildContent<HeaderTemplate>(c => c.Add(x => x.ChildContent, b => b.AddMarkupContent(0, "<strong id=\"h\">Custom</strong>")))
                .AddChildContent<ToolbarTemplate>(c => c.Add(x => x.ChildContent, b => b.AddMarkupContent(0, "<button id=\"tb\">Mine</button>"))));

            Assert.NotNull(cut.Find("#h"));
            Assert.NotNull(cut.Find(".dv-toolbar #tb"));
        }

        [Fact]
        public void Formats_values_with_format_formatter_and_template()
        {
            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Items, [new Row { Id = 1, Name = "a", Total = 1234.5m, Status = SourceTestStatus.OnHold }])
                .AddChildContent<DataColumn<Row, decimal>>(c => c.Add(x => x.Value, r => r.Total).Add(x => x.Format, "F2"))
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name).Add(x => x.Formatter, v => v.ToUpperInvariant()))
                .AddChildContent<DataColumn<Row, int>>(c => c.Add(x => x.Value, r => r.Id).Add(x => x.Template, r => b => b.AddMarkupContent(0, $"<b>#{r.Id}</b>")))
                .AddChildContent<DataColumn<Row, SourceTestStatus>>(c => c.Add(x => x.Value, r => r.Status)));

            var cells = cut.FindAll("tbody td");
            Assert.Equal(1234.5m.ToString("F2"), cells[0].TextContent.Trim());
            Assert.Equal("A", cells[1].TextContent.Trim());
            Assert.Equal("#1", cells[2].QuerySelector("b")!.TextContent);
            Assert.Equal("On hold", cells[3].TextContent.Trim());
        }

        [Fact]
        public void Two_columns_with_the_same_id_are_an_error()
        {
            var ex = Assert.ThrowsAny<Exception>(() => Render<DataView<Row>>(p => p
                .Add(x => x.Items, Row.Many(1))
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name))
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name.ToUpper()))));

            Assert.Contains("name", ex.Message);
        }

        [Fact]
        public void Needs_exactly_one_data_parameter()
        {
            Assert.Throws<InvalidOperationException>(() => RenderView(p => { }));
        }

        [Fact]
        public void Hidden_columns_are_not_rendered()
        {
            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Items, Row.Many(1))
                .AddChildContent<DataColumn<Row, int>>(c => c.Add(x => x.Value, r => r.Id).Add(x => x.Visible, false))
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name)));

            Assert.Single(cut.FindAll("thead th"));
            Assert.Equal(2, cut.Instance.Columns.Count);
        }

        // Sort

        [Fact]
        public void Clicking_a_sortable_header_sorts_ascending_then_descending()
        {
            var items = Row.Many(3);
            items.Reverse();
            var cut = RenderView(p => p.Add(x => x.Items, items));

            cut.Find("thead th:nth-child(2) button").Click();
            Assert.Equal(["Row 01", "Row 02", "Row 03"], CellTexts(cut, 1));
            Assert.Equal("ascending", cut.Find("thead th:nth-child(2)").GetAttribute("aria-sort"));

            cut.Find("thead th:nth-child(2) button").Click();
            Assert.Equal(["Row 03", "Row 02", "Row 01"], CellTexts(cut, 1));
            Assert.Equal("descending", cut.Find("thead th:nth-child(2)").GetAttribute("aria-sort"));
        }

        [Fact]
        public void Default_sort_is_sent_with_the_first_request()
        {
            var source = FakeSource<Row>.Answering(Row.Many(3));
            Render<DataView<Row>>(p => p
                .Add(x => x.Source, source)
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name).Add(x => x.Sortable, true).Add(x => x.DefaultSort, SortOrder.Descending)));

            Assert.Equal([new SortSpec("name", true)], source.Calls.Single().Request.Sorts);
        }

        [Fact]
        public void Key_is_added_as_the_last_sort_for_remote_sources()
        {
            var source = FakeSource<Row>.Answering(Row.Many(3));
            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Source, source)
                .Add(x => x.KeySelector, r => r.Id)
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name).Add(x => x.Sortable, true)));

            Assert.Equal([new SortSpec("id")], source.Calls[0].Request.Sorts);
            Assert.Contains(source.Calls[0].Request.Columns, c => c.Id == "id");

            cut.Find("thead button").Click();
            Assert.Equal([new SortSpec("name"), new SortSpec("id")], source.Last.Request.Sorts);
        }

        // Request lifecycle

        [Fact]
        public void Remote_source_shows_placeholders_until_the_first_page_arrives()
        {
            var source = new FakeSource<Row>();
            var cut = RenderView(p => p.Add(x => x.Source, source));

            Assert.NotEmpty(cut.FindAll("tr.dv-placeholder-row"));
            Assert.Single(source.Calls);

            cut.InvokeAsync(() => source.Last.Response.SetResult(new DataPage<Row>(Row.Many(2), 2)));

            cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("tbody tr").Count));
            Assert.Empty(cut.FindAll("tr.dv-placeholder-row"));
        }

        [Fact]
        public void Loading_template_replaces_placeholders()
        {
            var cut = RenderView(p => p
                .Add(x => x.Source, new FakeSource<Row>())
                .AddChildContent<LoadingTemplate>(c => c.Add(x => x.ChildContent, b => b.AddMarkupContent(0, "<span id=\"wait\">wait</span>"))));

            Assert.NotNull(cut.Find("#wait"));
        }

        [Fact]
        public void Later_loads_keep_the_rows_and_dim_them()
        {
            var source = new FakeSource<Row>();
            var cut = RenderView(p => p.Add(x => x.Source, source));
            cut.InvokeAsync(() => source.Last.Response.SetResult(new DataPage<Row>(Row.Many(2), 2)));
            cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("tbody tr").Count));

            cut.InvokeAsync(() => cut.Instance.ReloadAsync());

            cut.WaitForAssertion(() => Assert.NotNull(cut.Find("tbody.dv-loading")));
            Assert.Equal(2, cut.FindAll("tbody tr").Count);
            Assert.NotNull(cut.Find(".dv-progress"));
        }

        [Fact]
        public async Task A_new_load_cancels_and_waits_for_the_previous_one()
        {
            var source = new FakeSource<Row>();
            var cut = RenderView(p => p.Add(x => x.Source, source));
            var first = source.Last;

            _ = cut.InvokeAsync(() => cut.Instance.ReloadAsync());

            Assert.True(first.Token.IsCancellationRequested);
            Assert.Single(source.Calls);

            // The first source call ignores cancellation and answers late: its rows must not show.
            cut.InvokeAsync(() => first.Response.SetResult(new DataPage<Row>([new Row { Name = "stale" }], 1)));
            await source.WaitForCallsAsync(2);
            Assert.DoesNotContain(cut.FindAll("tbody td"), td => td.TextContent.Contains("stale"));

            cut.InvokeAsync(() => source.Last.Response.SetResult(new DataPage<Row>([new Row { Name = "fresh" }], 1)));
            cut.WaitForAssertion(() => Assert.Contains(cut.FindAll("tbody td"), td => td.TextContent.Contains("fresh")));
        }

        [Fact]
        public void Errors_show_the_message_and_retry_reloads()
        {
            var source = new FakeSource<Row>();
            var cut = RenderView(p => p.Add(x => x.Source, source));
            cut.InvokeAsync(() => source.Last.Response.SetException(new InvalidOperationException("Server down")));

            cut.WaitForAssertion(() => Assert.Contains("Server down", cut.Find("tr.dv-error").TextContent));
            Assert.Equal("Server down", cut.Instance.Error!.Message);

            cut.Find("button.dv-retry").Click();
            Assert.Equal(2, source.Calls.Count);
            cut.InvokeAsync(() => source.Last.Response.SetResult(new DataPage<Row>(Row.Many(1), 1)));
            cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("tr.dv-error")));
            Assert.Null(cut.Instance.Error);
        }

        [Fact]
        public void Error_template_gets_the_exception()
        {
            var source = new FakeSource<Row> { Respond = _ => throw new InvalidOperationException("boom") };
            var cut = RenderView(p => p
                .Add(x => x.Source, source)
                .AddChildContent<ErrorTemplate>(c => c.Add(x => x.ChildContent, ex => b => b.AddMarkupContent(0, $"<span id=\"err\">{ex.Message}</span>"))));

            cut.WaitForAssertion(() => Assert.Equal("boom", cut.Find("#err").TextContent));
        }

        [Fact]
        public void Empty_data_shows_no_data()
        {
            var cut = RenderView(p => p.Add(x => x.Items, new List<Row>()));

            Assert.NotNull(cut.Find("tr.dv-empty"));
            Assert.Empty(cut.FindAll(".dv-footer"));
        }

        [Fact]
        public void Empty_template_replaces_no_data()
        {
            var cut = RenderView(p => p
                .Add(x => x.Items, new List<Row>())
                .AddChildContent<EmptyTemplate>(c => c.Add(x => x.ChildContent, b => b.AddMarkupContent(0, "<span id=\"none\">none</span>"))));

            Assert.NotNull(cut.Find("#none"));
        }

        [Fact]
        public void No_results_offers_to_clear_the_search()
        {
            var cut = RenderView(p => p.Add(x => x.Items, Row.Many(3)).Add(x => x.SearchDelay, 0));

            cut.Find(".dv-search input").Input("nothing matches");
            Assert.NotNull(cut.Find("tr.dv-no-results"));

            cut.Find("button.dv-clear-filters").Click();
            Assert.Equal(3, cut.FindAll("tbody tr").Count);
            Assert.Equal("", cut.Find(".dv-search input").GetAttribute("value"));
        }

        // Search

        [Fact]
        public async Task Search_waits_for_the_delay_after_the_last_keystroke()
        {
            var source = FakeSource<Row>.Answering(Row.Many(30));
            var cut = RenderView(p => p.Add(x => x.Source, source));
            Assert.Single(source.Calls);

            cut.Find(".dv-search input").Input("Row");
            time.Advance(TimeSpan.FromMilliseconds(200));
            cut.Find(".dv-search input").Input("Row 1");
            time.Advance(TimeSpan.FromMilliseconds(200));
            Assert.Single(source.Calls);

            time.Advance(TimeSpan.FromMilliseconds(100));

            await source.WaitForCallsAsync(2);
            var search = source.Last.Request.Search!;
            Assert.Equal(["Row", "1"], search.Terms);
            Assert.Equal(["name"], search.Columns);
            Assert.Equal(0, source.Last.Request.Page.Skip);
        }

        [Fact]
        public void Search_matches_words_in_memory()
        {
            var cut = RenderView(p => p.Add(x => x.Items, Row.Many(30)).Add(x => x.SearchDelay, 0));

            cut.Find(".dv-search input").Input("row 2");

            Assert.Equal(["Row 02", "Row 12", "Row 20", "Row 21", "Row 22", "Row 23", "Row 24", "Row 25"], CellTexts(cut, 1).Take(8));
        }

        [Fact]
        public void Search_box_is_hidden_without_searchable_columns()
        {
            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Items, Row.Many(1))
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name)));

            Assert.Empty(cut.FindAll(".dv-search"));
        }

        [Fact]
        public async Task SetSearchAsync_searches_at_once()
        {
            var source = FakeSource<Row>.Answering(Row.Many(5));
            var cut = RenderView(p => p.Add(x => x.Source, source));

            await cut.InvokeAsync(() => cut.Instance.SetSearchAsync("\"Row 03\""));

            Assert.Equal(["Row 03"], source.Last.Request.Search!.Terms);
            Assert.Equal("\"Row 03\"", cut.Find(".dv-search input").GetAttribute("value"));
        }

        // Paging

        [Fact]
        public void Footer_shows_the_range_and_the_pager_moves_pages()
        {
            var source = FakeSource<Row>.Answering(Row.Many(30));
            var cut = RenderView(p => p.Add(x => x.Source, source).Add(x => x.PageSize, 10));

            Assert.Contains("Showing 1–10 of 30", cut.Find(".dv-showing").TextContent);
            Assert.Equal(["1", "2", "3"], cut.FindAll(".dv-pager .page-item:not(:first-child):not(:last-child)").Select(li => li.TextContent.Trim()));

            cut.Find(".dv-pager li:last-child button").Click();

            Assert.Equal(10, source.Last.Request.Page.Skip);
            Assert.Contains("Showing 11–20 of 30", cut.Find(".dv-showing").TextContent);
            Assert.Equal("2", cut.Find(".dv-pager .page-item.active").TextContent.Trim());
        }

        [Fact]
        public void Changing_page_size_goes_to_the_first_page()
        {
            var source = FakeSource<Row>.Answering(Row.Many(30));
            var cut = RenderView(p => p.Add(x => x.Source, source).Add(x => x.PageSize, 10));
            cut.Find(".dv-pager li:last-child button").Click();

            cut.Find(".dv-page-size select").Change("25");

            Assert.Equal(PageRequest.Offset(0, 25), source.Last.Request.Page);
        }

        [Fact]
        public void Cursor_paging_goes_forward_with_the_next_cursor_and_back_with_the_cached_one()
        {
            var memory = new InMemorySource<Row>(Row.Many(25), new InMemorySourceOptions { PagingMode = PagingMode.Cursor, IncludeTotalCount = false });
            var source = new FakeSource<Row> { PagingMode = PagingMode.Cursor, Respond = memory.GetPage };
            var cut = RenderView(p => p.Add(x => x.Source, source).Add(x => x.PageSize, 10));

            Assert.Contains("Showing 1–10", cut.Find(".dv-showing").TextContent);
            Assert.Equal(2, cut.FindAll(".dv-pager .page-item").Count);

            cut.Find(".dv-pager li:last-child button").Click();
            Assert.Equal(PageRequest.Cursor("10", 10), source.Last.Request.Page);
            cut.Find(".dv-pager li:last-child button").Click();
            Assert.Equal(PageRequest.Cursor("20", 10), source.Last.Request.Page);
            Assert.True(cut.Find(".dv-pager li:last-child button").HasAttribute("disabled"));

            cut.Find(".dv-pager li:first-child button").Click();
            Assert.Equal(PageRequest.Cursor("10", 10), source.Last.Request.Page);
            Assert.Contains("Showing 11–20", cut.Find(".dv-showing").TextContent);
        }

        [Fact]
        public async Task GoToPageAsync_goes_to_a_one_based_page()
        {
            var source = FakeSource<Row>.Answering(Row.Many(30));
            var cut = RenderView(p => p.Add(x => x.Source, source).Add(x => x.PageSize, 10));

            await cut.InvokeAsync(() => cut.Instance.GoToPageAsync(3));

            Assert.Equal(20, source.Last.Request.Page.Skip);
        }

        [Fact]
        public void Page_window_shows_first_last_and_neighbours()
        {
            Assert.Equal([0, 1, 2, null, 9], DataViewFooter<Row>.PageWindow(1, 10));
            Assert.Equal([0, null, 4, 5, 6, null, 9], DataViewFooter<Row>.PageWindow(5, 10));
            Assert.Equal([0, 1, 2], DataViewFooter<Row>.PageWindow(0, 3));
            Assert.Equal([0, 1, 2, 3, 4], DataViewFooter<Row>.PageWindow(2, 5));
        }

        // State and events

        [Fact]
        public void Initial_state_is_applied_to_the_first_request()
        {
            var source = FakeSource<Row>.Answering(Row.Many(30));
            RenderView(p => p
                .Add(x => x.Source, source)
                .Add(x => x.State, new DataViewState { Page = 2, PageSize = 5, Sorts = [new SortSpec("name", true)], Search = "Row" }));

            var request = source.Calls.Single().Request;
            Assert.Equal(PageRequest.Offset(5, 5), request.Page);
            Assert.Equal(new SortSpec("name", true), request.Sorts[0]);
            Assert.Equal(["Row"], request.Search!.Terms);
        }

        [Fact]
        public void State_ignores_sorts_on_unknown_columns()
        {
            var source = FakeSource<Row>.Answering(Row.Many(3));
            RenderView(p => p
                .Add(x => x.Source, source)
                .Add(x => x.State, new DataViewState { Sorts = [new SortSpec("secret")] }));

            Assert.Empty(source.Calls.Single().Request.Sorts);
        }

        [Fact]
        public void StateChanged_is_raised_when_the_user_sorts()
        {
            DataViewState? state = null;
            var cut = RenderView(p => p
                .Add(x => x.Items, Row.Many(3))
                .Add(x => x.StateChanged, s => state = s));

            cut.Find("thead th:nth-child(1) button").Click();

            Assert.Equal(new DataViewState { Page = 1, PageSize = 25, Sorts = [new SortSpec("id")] }, state);
        }

        [Fact]
        public void State_equality_compares_values()
        {
            var a = new DataViewState { Sorts = [new SortSpec("a")], Filters = [new FilterSpec("b", FilterOperator.In, new[] { 1, 2 })] };
            var b = new DataViewState { Sorts = [new SortSpec("a")], Filters = [new FilterSpec("b", FilterOperator.In, new List<int> { 1, 2 })] };

            Assert.Equal(a, b);
            Assert.NotEqual(a, b with { Page = 2 });
        }

        [Fact]
        public void OnPageLoaded_is_raised_with_the_page()
        {
            DataPage<Row>? loaded = null;
            RenderView(p => p.Add(x => x.Items, Row.Many(3)).Add(x => x.OnPageLoaded, page => loaded = page));

            Assert.Equal(3, loaded!.TotalCount);
        }

        [Fact]
        public async Task ReloadAsync_loads_again()
        {
            var source = FakeSource<Row>.Answering(Row.Many(3));
            var cut = RenderView(p => p.Add(x => x.Source, source));

            await cut.InvokeAsync(() => cut.Instance.ReloadAsync());

            Assert.Equal(2, source.Calls.Count);
        }

        [Fact]
        public void A_new_items_list_reloads()
        {
            var cut = RenderView(p => p.Add(x => x.Items, Row.Many(2)));

            cut.Render(p => p.Add(x => x.Items, Row.Many(4)));

            Assert.Equal(4, cut.FindAll("tbody tr").Count);
        }

        [Fact]
        public async Task ItemsProvider_is_called_with_the_request()
        {
            DataRequest? received = null;
            var cut = RenderView(p => p.Add(x => x.ItemsProvider, (request, _) =>
            {
                received = request;
                return Task.FromResult(new DataPage<Row>(Row.Many(2), 2));
            }));

            await cut.InvokeAsync(() => { });
            cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("tbody tr").Count));
            Assert.Equal(PageRequest.Offset(0, 25), received!.Page);
        }

        [Fact]
        public void Refresh_button_is_shown_for_remote_sources_only()
        {
            Assert.Empty(RenderView(p => p.Add(x => x.Items, Row.Many(1))).FindAll("button.dv-refresh"));
            Assert.Single(RenderView(p => p.Add(x => x.Source, FakeSource<Row>.Answering(Row.Many(1)))).FindAll("button.dv-refresh"));
        }

        // Rows and styling

        [Fact]
        public void RowHref_makes_rows_real_links()
        {
            var cut = RenderView(p => p.Add(x => x.Items, Row.Many(2)).Add(x => x.RowHref, r => $"/rows/{r.Id}"));

            var anchors = cut.FindAll("tbody tr a.dv-row-anchor");
            Assert.Equal(["/rows/1", "/rows/2"], anchors.Select(a => a.GetAttribute("href")));
            Assert.Contains("dv-row-link", cut.Find("tbody tr").ClassList);
        }

        [Fact]
        public void OnRowClicked_receives_the_item()
        {
            Row? clicked = null;
            var cut = RenderView(p => p.Add(x => x.Items, Row.Many(2)).Add(x => x.OnRowClicked, r => clicked = r));

            cut.FindAll("tbody tr")[1].Click();

            Assert.Equal(2, clicked!.Id);
        }

        [Fact]
        public void Row_color_class_and_style_come_from_the_item()
        {
            var cut = RenderView(p => p
                .Add(x => x.Items, Row.Many(2))
                .Add(x => x.RowColor, r => r.Id == 1 ? TablerColor.Danger : TablerColor.Red)
                .Add(x => x.RowClass, r => $"r{r.Id}")
                .Add(x => x.RowStyle, r => "opacity: .5"));

            var rows = cut.FindAll("tbody tr");
            Assert.Contains("table-danger", rows[0].ClassList);
            Assert.Contains("bg-red-lt", rows[1].ClassList);
            Assert.Contains("r2", rows[1].ClassList);
            Assert.Equal("opacity: .5", rows[0].GetAttribute("style"));
        }

        [Fact]
        public void Cell_styling_and_sizing_come_from_the_column()
        {
            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Items, [new Row { Id = 1, Name = "A long name", Total = -5 }])
                .AddChildContent<DataColumn<Row, decimal>>(c => c
                    .Add(x => x.Value, r => r.Total)
                    .Add(x => x.Align, Align.End)
                    .Add(x => x.Width, "120px")
                    .Add(x => x.MinWidth, "80px")
                    .Add(x => x.Class, "static")
                    .Add(x => x.HeaderClass, "head")
                    .Add(x => x.CellColor, r => r.Total < 0 ? TablerColor.Red : null)
                    .Add(x => x.CellClass, r => "fw-bold")
                    .Add(x => x.CellStyle, r => "color: red"))
                .AddChildContent<DataColumn<Row, string>>(c => c
                    .Add(x => x.Value, r => r.Name).Add(x => x.Truncate, true).Add(x => x.NoWrap, true).Add(x => x.Width, "50px")));

            var th = cut.Find("thead th");
            Assert.Equal("width: 120px;min-width: 80px;", th.GetAttribute("style"));
            Assert.Contains("head", th.ClassList);
            Assert.Contains("text-end", th.ClassList);

            var cells = cut.FindAll("tbody td");
            Assert.Equal(["dv-first-col", "static", "text-end", "bg-red-lt", "fw-bold"], cells[0].ClassList);
            Assert.Equal("color: red", cells[0].GetAttribute("style"));
            Assert.Contains("text-truncate", cells[1].ClassList);
            Assert.Contains("text-nowrap", cells[1].ClassList);
            Assert.Equal("A long name", cells[1].GetAttribute("title"));
        }

        [Fact]
        public void Table_look_parameters_set_classes_and_unknown_attributes_pass_through()
        {
            var cut = RenderView(p => p
                .Add(x => x.Items, Row.Many(1))
                .Add(x => x.Striped, true)
                .Add(x => x.Bordered, true)
                .Add(x => x.Hover, true)
                .Add(x => x.Density, TableDensity.Compact)
                .Add(x => x.StickyHeader, true)
                .Add(x => x.TableClass, "extra")
                .AddUnmatched("data-test", "orders"));

            var table = cut.Find("table");
            foreach (var css in new[] { "table-striped", "table-bordered", "table-hover", "table-sm", "dv-sticky-header", "extra" })
            {
                Assert.Contains(css, table.ClassList);
            }

            Assert.Equal("orders", table.GetAttribute("data-test"));
            Assert.Empty(cut.FindAll("button.dv-menu"));
        }

        [Fact]
        public void Density_menu_changes_the_density()
        {
            var cut = RenderView(p => p.Add(x => x.Items, Row.Many(1)));

            cut.Find("button.dv-menu").Click();
            cut.FindAll(".dropdown-item")[0].Click();

            Assert.Contains("table-sm", cut.Find("table").ClassList);
        }

        [Fact]
        public void Rows_are_not_re_rendered_while_typing_a_search()
        {
            var renders = 0;
            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Items, Row.Many(2))
                .AddChildContent<DataColumn<Row, string>>(c => c
                    .Add(x => x.Value, r => r.Name).Add(x => x.Searchable, true)
                    .Add(x => x.Template, r => b => { renders++; b.AddContent(0, r.Name); })));
            var before = renders;

            cut.Find(".dv-search input").Input("R");

            Assert.Equal(before, renders);
        }
    }
}
