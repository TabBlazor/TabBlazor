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
    public class DataViewFilterTests : TabBlazorTestContext
    {
        private readonly FakeTimeProvider time = new();
        private readonly FakeSource<Row> source = FakeSource<Row>.Answering(Row.Many(30));

        public DataViewFilterTests()
        {
            Services.AddSingleton<TimeProvider>(time);
        }

        private IRenderedComponent<DataView<Row>> RenderFilterable(Action<ComponentParameterCollectionBuilder<DataView<Row>>>? configure = null, int searchDelay = 0) =>
            Render<DataView<Row>>(p =>
            {
                p.Add(x => x.Source, source)
                    .Add(x => x.PageSize, 50)
                    .Add(x => x.SearchDelay, searchDelay)
                    .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name).Add(x => x.Filterable, true))
                    .AddChildContent<DataColumn<Row, decimal>>(c => c.Add(x => x.Value, r => r.Total).Add(x => x.Filterable, true))
                    .AddChildContent<DataColumn<Row, SourceTestStatus>>(c => c.Add(x => x.Value, r => r.Status).Add(x => x.Filterable, true))
                    .AddChildContent<DataColumn<Row, bool>>(c => c.Add(x => x.Value, r => r.Active).Add(x => x.Filterable, true))
                    .AddChildContent<DataColumn<Row, DateTime>>(c => c.Add(x => x.Value, r => r.Date).Add(x => x.Filterable, true))
                    .AddChildContent<DataColumn<Row, int?>>(c => c.Add(x => x.Value, r => r.Score).Add(x => x.Filterable, true))
                    .AddChildContent<DataColumn<Row, int>>(c => c.Add(x => x.Value, r => r.Id));
                configure?.Invoke(p);
            });

        private static void OpenFilter(IRenderedComponent<DataView<Row>> cut, string columnId) =>
            cut.Find($"thead th[data-column-id='{columnId}'] .dv-filter-button").Click();

        private IReadOnlyList<FilterSpec> LastFilters => source.Last.Request.Filters;

        [Fact]
        public void Default_operators_follow_the_value_type()
        {
            var cut = RenderFilterable();
            var columns = cut.Instance.Columns.ToDictionary(c => c.Id);

            Assert.Equal([FilterOperator.Contains], columns["name"].AllowedFilterOperators);
            Assert.Equal([FilterOperator.Between], columns["total"].AllowedFilterOperators);
            Assert.Equal([FilterOperator.In], columns["status"].AllowedFilterOperators);
            Assert.Equal([FilterOperator.Equals], columns["active"].AllowedFilterOperators);
            Assert.Equal([FilterOperator.Between], columns["date"].AllowedFilterOperators);
            Assert.Equal([FilterOperator.Between, FilterOperator.IsNull, FilterOperator.IsNotNull], columns["score"].AllowedFilterOperators);
            Assert.Empty(columns["id"].AllowedFilterOperators);
        }

        [Fact]
        public void Text_filter_applies_contains_from_the_header_menu()
        {
            var cut = RenderFilterable();
            Assert.Empty(cut.FindAll("thead th[data-column-id='id'] .dv-filter-button"));

            OpenFilter(cut, "name");
            cut.Find("thead .dv-column-filter input").Input("Row 1");

            Assert.Equal([new FilterSpec("name", FilterOperator.Contains, "Row 1")], LastFilters);
            Assert.Contains("dv-filter-active", cut.Find("thead th[data-column-id='name'] .dv-filter-button").ClassList);
        }

        [Fact]
        public async Task Text_filter_waits_for_a_typing_pause()
        {
            var cut = RenderFilterable(searchDelay: 300);
            var calls = source.Calls.Count;

            OpenFilter(cut, "name");
            cut.Find("thead .dv-column-filter input").Input("Row");
            Assert.Equal(calls, source.Calls.Count);

            time.Advance(TimeSpan.FromMilliseconds(300));
            await source.WaitForCallsAsync(calls + 1);
            Assert.Equal("Row", LastFilters.Single().Value);
        }

        [Fact]
        public void Enum_filter_uses_checkboxes_and_in()
        {
            var cut = RenderFilterable();

            OpenFilter(cut, "status");
            var boxes = cut.FindAll("thead .dv-column-filter input[type=checkbox]");
            Assert.Equal(3, boxes.Count);
            boxes[1].Change(true);
            cut.FindAll("thead .dv-column-filter input[type=checkbox]")[2].Change(true);

            var filter = LastFilters.Single();
            Assert.Equal(FilterOperator.In, filter.Operator);
            Assert.Equal([SourceTestStatus.OnHold, SourceTestStatus.Closed], DataFilters.AsList(filter.Value).Cast<SourceTestStatus>());
        }

        [Fact]
        public void Number_range_converts_to_the_column_type()
        {
            var cut = RenderFilterable();

            OpenFilter(cut, "total");
            cut.FindAll("thead .dv-filter-range input")[0].Change("100");

            Assert.Equal(new FilterSpec("total", FilterOperator.Between, 100m, null), LastFilters.Single());
        }

        [Fact]
        public void Date_range_includes_the_whole_last_day()
        {
            var cut = RenderFilterable();

            OpenFilter(cut, "date");
            cut.FindAll("thead .dv-filter-range input")[1].Change("2026-01-10");

            var filter = LastFilters.Single();
            Assert.Null(filter.Value);
            Assert.Equal(new DateTime(2026, 1, 10, 23, 59, 59).AddTicks(9_999_999), filter.Value2);
        }

        [Fact]
        public void Bool_filter_offers_any_yes_and_no()
        {
            var cut = RenderFilterable();

            OpenFilter(cut, "active");
            cut.FindAll("thead .dv-filter-bool button")[2].Click();
            Assert.Equal(new FilterSpec("active", FilterOperator.Equals, false), LastFilters.Single());

            cut.FindAll("thead .dv-filter-bool button")[0].Click();
            Assert.Empty(LastFilters);
        }

        [Fact]
        public void Nullable_columns_can_filter_empty_values()
        {
            var cut = RenderFilterable();

            OpenFilter(cut, "score");
            cut.Find("thead .dv-filter-operator").Change(nameof(FilterOperator.IsNull));

            Assert.Equal(new FilterSpec("score", FilterOperator.IsNull), LastFilters.Single());
            Assert.Empty(cut.FindAll("thead th[data-column-id='score'] .dv-column-filter input"));
        }

        [Fact]
        public void Filter_operators_add_an_operator_picker()
        {
            var cut = RenderFilterable(p => p.AddChildContent<DataColumn<Row, string>>(c => c
                .Add(x => x.Value, r => r.Name)
                .Add(x => x.Id, "shout")
                .Add(x => x.Filterable, true)
                .Add(x => x.FilterOperators, [FilterOperator.StartsWith, FilterOperator.Equals, FilterOperator.GreaterThan])));

            var shout = cut.Instance.Columns.Single(c => c.Id == "shout");
            Assert.Equal([FilterOperator.StartsWith, FilterOperator.Equals], shout.AllowedFilterOperators);
        }

        [Fact]
        public void Clear_removes_the_column_filter()
        {
            var cut = RenderFilterable();
            OpenFilter(cut, "name");
            cut.Find("thead .dv-column-filter input").Input("Row");

            cut.Find("thead .dv-filter-clear").Click();

            Assert.Empty(LastFilters);
        }

        [Fact]
        public async Task Filters_button_shows_the_count_and_lists_every_filterable_column()
        {
            var cut = RenderFilterable();
            await cut.InvokeAsync(() => cut.Instance.SetFiltersAsync([new FilterSpec("status", FilterOperator.In, new[] { SourceTestStatus.Open })]));

            Assert.Equal("1", cut.Find(".dv-filters-button .badge").TextContent);
            cut.Find(".dv-filters-button").Click();

            var labels = cut.FindAll(".dv-filters-panel label.form-label").Select(l => l.TextContent).ToList();
            Assert.Equal(6, labels.Count);
            Assert.Equal("Status", labels[0]);

            cut.Find(".dv-filters-panel .dv-filters-clear-all").Click();
            Assert.Empty(LastFilters);
        }

        [Fact]
        public void Column_filter_menu_has_a_title_and_closes_with_the_x()
        {
            var cut = RenderFilterable();
            OpenFilter(cut, "name");

            Assert.Equal("Name", cut.Find(".dv-filter-menu-card .dv-popup-header").TextContent.Trim());
            Assert.Equal("Close", cut.Find(".dv-filter-menu-card .dv-popup-close").GetAttribute("aria-label"));

            cut.Find(".dv-filter-menu-card .dv-popup-close").Click();

            Assert.Empty(cut.FindAll(".dv-filter-menu-card"));
        }

        [Fact]
        public void Filters_menu_closes_with_the_x()
        {
            var cut = RenderFilterable();
            cut.Find(".dv-filters-button").Click();

            Assert.Equal("Filters", cut.Find(".dv-filters-menu .dv-popup-header").TextContent.Trim());

            cut.Find(".dv-filters-menu .dv-popup-close").Click();

            Assert.Empty(cut.FindAll(".dv-filters-panel"));
        }

        [Fact]
        public async Task Chips_show_active_filters_and_remove_them()
        {
            var cut = RenderFilterable(p => p.Add(x => x.ShowFilterChips, true));
            await cut.InvokeAsync(() => cut.Instance.SetFiltersAsync(
            [
                new FilterSpec("status", FilterOperator.In, new[] { SourceTestStatus.OnHold }),
                new FilterSpec("total", FilterOperator.Between, 100, 200)
            ]));

            var chips = cut.FindAll(".dv-filter-chip");
            Assert.Equal(2, chips.Count);
            Assert.Contains("Status: On hold", chips[0].TextContent);
            Assert.Contains("–", chips[1].TextContent);

            chips[0].QuerySelector(".btn-close")!.Click();
            Assert.Equal(["total"], LastFilters.Select(f => f.Column));
        }

        [Fact]
        public void Row_mode_renders_a_filter_row()
        {
            var cut = RenderFilterable(p => p.Add(x => x.FilterMode, DataFilterMode.Row));

            Assert.Empty(cut.FindAll("thead .dv-filter-button"));
            var row = cut.Find("thead tr.dv-filter-row");
            Assert.Equal(7, row.QuerySelectorAll("th").Length);
            row.QuerySelector("input")!.Input("Row 2");
            Assert.Equal("name", LastFilters.Single().Column);
        }

        [Fact]
        public void Filters_parameter_sets_the_first_request_and_changes_are_reported()
        {
            IReadOnlyList<FilterSpec>? changed = null;
            var cut = RenderFilterable(p => p
                .Add(x => x.Filters, [new FilterSpec("total", FilterOperator.Between, 50, null)])
                .Add(x => x.FiltersChanged, f => changed = f));

            Assert.Equal(new FilterSpec("total", FilterOperator.Between, 50m, null), source.Calls[0].Request.Filters.Single());

            cut.Find(".dv-filters-button").Click();
            cut.Find(".dv-filters-panel .dv-filters-clear-all").Click();

            Assert.Empty(changed!);
        }

        [Fact]
        public async Task Filters_on_unknown_columns_or_disallowed_operators_are_ignored()
        {
            var cut = RenderFilterable();

            await cut.InvokeAsync(() => cut.Instance.SetFiltersAsync(
            [
                new FilterSpec("id", FilterOperator.Equals, 1),
                new FilterSpec("secret", FilterOperator.Equals, 1),
                new FilterSpec("name", FilterOperator.GreaterThan, "x"),
                new FilterSpec("total", FilterOperator.Between, "not a number", null),
                new FilterSpec("name", FilterOperator.Contains, "ok")
            ]));

            Assert.Equal([new FilterSpec("name", FilterOperator.Contains, "ok")], LastFilters);
        }

        [Fact]
        public void Filter_template_replaces_the_control()
        {
            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Source, source)
                .AddChildContent<DataColumn<Row, string>>(c => c
                    .Add(x => x.Value, r => r.Name)
                    .Add(x => x.Filterable, true)
                    .Add(x => x.FilterTemplate, ctx => b =>
                    {
                        b.OpenElement(0, "button");
                        b.AddAttribute(1, "class", "only-row-1");
                        b.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, () => ctx.SetAsync(new FilterSpec("name", FilterOperator.Contains, "Row 01"))));
                        b.CloseElement();
                    })));

            cut.Find(".dv-filter-button").Click();
            cut.Find("button.only-row-1").Click();

            Assert.Equal("Row 01", LastFilters.Single().Value);
        }

        [Fact]
        public void Small_screens_filter_in_a_bottom_sheet()
        {
            JSInterop.Setup<bool>("tabBlazor.watchMedia", _ => true).SetResult(true);
            var cut = RenderFilterable();

            cut.Find(".dv-filters-button").Click();

            Assert.Contains("offcanvas-bottom", cut.Find(".offcanvas").ClassList);
            Assert.NotNull(cut.Find(".offcanvas .dv-filters-panel"));
        }

        [Fact]
        public void The_filters_panel_keeps_its_order_while_a_filter_applies()
        {
            var cut = RenderFilterable();
            cut.Find(".dv-filters-button").Click();
            List<string> Labels() => cut.FindAll(".dv-filters-panel label.form-label").Select(l => l.TextContent).ToList();
            var before = Labels();

            cut.FindAll(".dv-filters-panel-item")[3].QuerySelectorAll(".dv-filter-bool button")[1].Click();

            Assert.NotNull(cut.Instance.GetFilter("active"));
            Assert.Equal(before, Labels());
            Assert.Contains("active", cut.FindAll(".dv-filters-panel-item")[3].QuerySelectorAll(".dv-filter-bool button")[1].ClassList);
        }

        [Fact]
        public async Task Clearing_all_filters_drops_a_typed_filter_that_has_not_applied_yet()
        {
            var cut = RenderFilterable(searchDelay: 300);
            OpenFilter(cut, "name");
            cut.Find("thead .dv-column-filter input").Input("Row");
            await cut.InvokeAsync(() => cut.Instance.ClearFiltersAsync());
            var calls = source.Calls.Count;

            time.Advance(TimeSpan.FromMilliseconds(300));
            await Task.Delay(50);

            Assert.Equal(calls, source.Calls.Count);
            Assert.Null(cut.Instance.GetFilter("name"));
        }

        [Fact]
        public async Task Switching_from_in_to_equals_keeps_the_values_as_text()
        {
            var cut = RenderFilterable(p => p.AddChildContent<DataColumn<Row, string>>(c => c
                .Add(x => x.Value, r => r.Name)
                .Add(x => x.Id, "code")
                .Add(x => x.Filterable, true)
                .Add(x => x.FilterOperators, [FilterOperator.In, FilterOperator.Equals])));
            await cut.InvokeAsync(() => cut.Instance.SetColumnFilterAsync("code", new FilterSpec("code", FilterOperator.In, new[] { "Row 01", "Row 02" })));
            OpenFilter(cut, "code");

            cut.Find("thead .dv-filter-operator").Change(nameof(FilterOperator.Equals));

            var filter = cut.Instance.GetFilter("code")!;
            Assert.Equal(FilterOperator.Equals, filter.Operator);
            Assert.Equal("Row 01,Row 02", filter.Value);
        }
    }
}
