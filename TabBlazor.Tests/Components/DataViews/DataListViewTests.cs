#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace TabBlazor.Tests.Components.DataViews
{
    public class DataListViewTests : TabBlazorTestContext
    {
        private IRenderedComponent<DataView<Row>> RenderList(Action<ComponentParameterCollectionBuilder<DataView<Row>>> configure) =>
            Render<DataView<Row>>(p =>
            {
                p.Add(x => x.View, DataViewKind.List);
                configure(p);
                p.AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name).Add(x => x.Sortable, true).Add(x => x.Searchable, true));
                p.AddChildContent<DataColumn<Row, decimal>>(c => c.Add(x => x.Value, r => r.Total).Add(x => x.Format, "F1").Add(x => x.Sortable, true));
            });

        [Fact]
        public void Renders_items_with_the_template()
        {
            var cut = RenderList(p => p
                .Add(x => x.Items, Row.Many(3))
                .AddChildContent<ListItemTemplate<Row>>(c => c.Add(x => x.ChildContent, r => b => b.AddContent(0, $"#{r.Id} {r.Name}"))));

            Assert.Empty(cut.FindAll("table"));
            Assert.Equal(["#1 Row 01", "#2 Row 02", "#3 Row 03"], cut.FindAll(".dv-list-item").Select(e => e.TextContent.Trim()));
        }

        [Fact]
        public void Without_a_template_columns_build_label_value_lines()
        {
            var cut = RenderList(p => p.Add(x => x.Items, Row.Many(1)));

            var item = cut.Find(".dv-list-item");
            Assert.Equal("Row 01", item.QuerySelector(".dv-list-title")!.TextContent.Trim());
            Assert.Equal("Total:", item.QuerySelector(".dv-list-label")!.TextContent.Trim());
            Assert.Contains(10.5m.ToString("F1"), item.QuerySelector(".dv-list-field")!.TextContent);
        }

        [Fact]
        public void RowHref_renders_items_as_links()
        {
            var cut = RenderList(p => p.Add(x => x.Items, Row.Many(2)).Add(x => x.RowHref, r => $"/rows/{r.Id}"));

            var links = cut.FindAll("a.dv-list-item");
            Assert.Equal(["/rows/1", "/rows/2"], links.Select(a => a.GetAttribute("href")));
            Assert.Contains("list-group-item-action", links[0].ClassList);
        }

        [Fact]
        public void Row_color_uses_light_backgrounds()
        {
            var cut = RenderList(p => p.Add(x => x.Items, Row.Many(1)).Add(x => x.RowColor, _ => TablerColor.Danger));

            Assert.Contains("bg-danger-lt", cut.Find(".dv-list-item").ClassList);
        }

        [Fact]
        public void OnRowClicked_receives_the_item()
        {
            Row? clicked = null;
            var cut = RenderList(p => p.Add(x => x.Items, Row.Many(2)).Add(x => x.OnRowClicked, r => clicked = r));

            cut.FindAll(".dv-list-item")[1].Click();

            Assert.Equal(2, clicked!.Id);
        }

        [Fact]
        public void Shows_states_like_the_table()
        {
            Assert.NotNull(RenderList(p => p.Add(x => x.Items, new List<Row>())).Find(".dv-list .dv-empty"));

            var source = new FakeSource<Row> { Respond = _ => throw new InvalidOperationException("down") };
            var cut = RenderList(p => p.Add(x => x.Source, source));
            cut.WaitForAssertion(() => Assert.Contains("down", cut.Find(".dv-list .dv-error").TextContent));
        }

        [Fact]
        public void Shows_placeholders_on_the_first_load()
        {
            var cut = RenderList(p => p.Add(x => x.Source, new FakeSource<Row>()));

            Assert.NotEmpty(cut.FindAll(".dv-list .dv-placeholder-row"));
        }

        [Fact]
        public void Sort_dropdown_sorts_by_the_chosen_column()
        {
            var items = Row.Many(3);
            items.Reverse();
            var cut = RenderList(p => p.Add(x => x.Items, items));

            cut.Find(".dv-sort-menu button").Click();
            cut.FindAll(".dv-sort-menu .dropdown-item")[0].Click();

            Assert.Equal(["Row 01", "Row 02", "Row 03"], cut.FindAll(".dv-list-title").Select(e => e.TextContent.Trim()));
            Assert.Contains("Name", cut.Find(".dv-sort-menu button").TextContent);
        }

        [Fact]
        public void Table_view_has_no_sort_dropdown()
        {
            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Items, Row.Many(1))
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name).Add(x => x.Sortable, true)));

            Assert.Empty(cut.FindAll(".dv-sort-menu"));
            Assert.Empty(cut.FindAll(".dv-view-switcher"));
        }

        [Fact]
        public void View_switcher_switches_views_and_keeps_the_page()
        {
            DataViewKind? changed = null;
            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Items, Row.Many(30))
                .Add(x => x.PageSize, 10)
                .Add(x => x.Views, DataViewKind.Table | DataViewKind.List)
                .Add(x => x.ViewChanged, v => changed = v)
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name)));
            cut.Find(".dv-pager li:last-child button").Click();

            var buttons = cut.FindAll(".dv-view-switcher button");
            Assert.Equal("true", buttons[0].GetAttribute("aria-pressed"));
            buttons[1].Click();

            Assert.Equal(DataViewKind.List, changed);
            Assert.Equal(DataViewKind.List, cut.Instance.CurrentView);
            Assert.Equal("Row 11", cut.Find(".dv-list-title").TextContent.Trim());
            Assert.Equal("true", cut.FindAll(".dv-view-switcher button")[1].GetAttribute("aria-pressed"));
        }

        [Fact]
        public void Bound_view_parameter_wins()
        {
            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Items, Row.Many(1))
                .Add(x => x.View, DataViewKind.Table)
                .Add(x => x.Views, DataViewKind.Table | DataViewKind.List)
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name)));

            cut.Render(p => p.Add(x => x.View, DataViewKind.List));

            Assert.NotEmpty(cut.FindAll(".dv-list-item"));
        }

        [Fact]
        public void DataListItem_renders_its_parts()
        {
            var cut = Render<DataListItem>(p => p
                .Add(x => x.Avatar, b => b.AddMarkupContent(0, "<span class=\"avatar\">A</span>"))
                .Add(x => x.Title, b => b.AddContent(0, "Ada Lovelace"))
                .Add(x => x.Subtitle, b => b.AddContent(0, "Odin AB"))
                .Add(x => x.Meta, b => b.AddContent(0, "2026-01-15"))
                .AddChildContent("<span class=\"extra\">x</span>"));

            Assert.NotNull(cut.Find(".col-auto .avatar"));
            Assert.Equal("Ada Lovelace", cut.Find(".fw-medium").TextContent);
            Assert.Equal("Odin AB", cut.Find(".text-secondary.text-truncate").TextContent);
            Assert.Equal("2026-01-15", cut.Find(".text-end").TextContent);
            Assert.NotNull(cut.Find(".extra"));
        }

        [Fact]
        public void DataListItem_skips_missing_parts()
        {
            var cut = Render<DataListItem>(p => p.Add(x => x.Title, b => b.AddContent(0, "Only title")));

            Assert.Single(cut.FindAll(".col, .col-auto"));
        }
    }
}
