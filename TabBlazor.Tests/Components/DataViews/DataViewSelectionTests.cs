#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace TabBlazor.Tests.Components.DataViews
{
    public class DataViewSelectionTests : TabBlazorTestContext
    {
        private IReadOnlyList<Row> selectedItems = [];
        private DataSelection<Row>? lastSelection;

        private IRenderedComponent<DataView<Row>> RenderSelectable(
            Action<ComponentParameterCollectionBuilder<DataView<Row>>> configure,
            DataSelectionMode mode = DataSelectionMode.Multiple)
        {
            return Render<DataView<Row>>(p =>
            {
                p.Add(x => x.SelectionMode, mode)
                    .Add(x => x.KeySelector, r => r.Id)
                    .Add(x => x.SelectedItemsChanged, items => selectedItems = items)
                    .Add(x => x.OnSelectionChanged, s => lastSelection = s)
                    .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name).Add(x => x.Searchable, true));
                configure(p);
            });
        }

        private static void Tick(IRenderedComponent<DataView<Row>> cut, int row) =>
            cut.FindAll("tbody tr")[row].QuerySelector("input.dv-select")!.Change(true);

        [Fact]
        public void Checkboxes_select_rows_by_key()
        {
            var cut = RenderSelectable(p => p.Add(x => x.Items, Row.Many(3)));

            Tick(cut, 1);

            Assert.Equal([2], selectedItems.Select(r => r.Id));
            var row = cut.FindAll("tbody tr")[1];
            Assert.Contains("table-active", row.ClassList);
            Assert.Equal("true", row.GetAttribute("aria-selected"));
            Assert.Equal([2], cut.Instance.GetSelectedKeys());
        }

        [Fact]
        public void Selection_survives_reloads_with_new_instances()
        {
            var source = new FakeSource<Row> { Respond = _ => new DataPage<Row>(Row.Many(3), 3) };
            var cut = RenderSelectable(p => p.Add(x => x.Source, source));
            Tick(cut, 0);
            var before = selectedItems.Single();

            cut.InvokeAsync(() => cut.Instance.ReloadAsync());

            Assert.True(cut.FindAll("tbody tr")[0].QuerySelector("input.dv-select")!.HasAttribute("checked"));
            Assert.NotSame(before, cut.Instance.GetSelection().Items.Single());
        }

        [Fact]
        public void Selection_survives_paging()
        {
            var cut = RenderSelectable(p => p.Add(x => x.Items, Row.Many(30)).Add(x => x.PageSize, 10));
            Tick(cut, 0);

            cut.Find(".dv-pager li:last-child button").Click();
            Tick(cut, 0);
            cut.Find(".dv-pager li:first-child button").Click();

            Assert.Equal([1, 11], cut.Instance.GetSelectedKeys().Cast<int>().OrderBy(k => k));
            Assert.True(cut.FindAll("tbody tr")[0].QuerySelector("input.dv-select")!.HasAttribute("checked"));
        }

        [Fact]
        public void Selected_items_parameter_sets_the_selection()
        {
            var items = Row.Many(3);
            var cut = RenderSelectable(p => p.Add(x => x.Items, items).Add(x => x.SelectedItems, [items[2]]));

            Assert.Contains("table-active", cut.FindAll("tbody tr")[2].ClassList);
        }

        [Fact]
        public void Selection_bar_replaces_the_header_and_clears()
        {
            var cut = RenderSelectable(p => p.Add(x => x.Items, Row.Many(3)).Add(x => x.Title, "Orders"));
            Tick(cut, 0);
            Tick(cut, 1);

            Assert.Contains("2 selected", cut.Find(".dv-selection-bar").TextContent);
            Assert.Empty(cut.FindAll(".card-title"));

            cut.Find("button.dv-clear-selection").Click();

            Assert.Empty(selectedItems);
            Assert.Empty(cut.FindAll(".dv-selection-bar"));
        }

        [Fact]
        public async Task Select_all_matching_carries_the_request()
        {
            var cut = RenderSelectable(p => p.Add(x => x.Items, Row.Many(30)).Add(x => x.PageSize, 10));
            await cut.InvokeAsync(() => cut.Instance.TogglePageSelectionAsync());

            cut.Find("button.dv-select-all-matching").Click();

            Assert.True(lastSelection!.AllMatching);
            Assert.Equal(30, lastSelection.Count);
            Assert.Equal(PageRequest.Offset(0, 30), lastSelection.Request!.Page);
            Assert.Contains("All 30 matching", cut.Find(".dv-selection-bar").TextContent);
        }

        [Fact]
        public async Task Searching_leaves_all_matching_mode()
        {
            var cut = RenderSelectable(p => p.Add(x => x.Items, Row.Many(30)).Add(x => x.PageSize, 10));
            await cut.InvokeAsync(() => cut.Instance.TogglePageSelectionAsync());
            cut.Find("button.dv-select-all-matching").Click();

            await cut.InvokeAsync(() => cut.Instance.SetSearchAsync("Row 1"));

            Assert.False(cut.Instance.GetSelection().AllMatching);
        }

        [Fact]
        public void Single_selection_selects_the_clicked_row()
        {
            var cut = RenderSelectable(p => p.Add(x => x.Items, Row.Many(3)), DataSelectionMode.Single);

            Assert.Empty(cut.FindAll("input.dv-select"));
            cut.FindAll("tbody tr")[0].Click();
            cut.FindAll("tbody tr")[2].Click();

            Assert.Equal([3], selectedItems.Select(r => r.Id));
        }

        [Fact]
        public void Bulk_actions_receive_the_selection()
        {
            DataSelection<Row>? received = null;
            var cut = RenderSelectable(p => p
                .Add(x => x.Items, Row.Many(3))
                .AddChildContent<BulkActions<Row>>(b => b.AddChildContent<BulkAction<Row>>(a => a
                    .Add(x => x.Title, "Export")
                    .Add(x => x.OnClick, s =>
                    {
                        received = s;
                        return Task.CompletedTask;
                    }))));
            Tick(cut, 0);
            Tick(cut, 2);

            cut.Find("button.dv-bulk-action").Click();

            Assert.Equal([1, 3], received!.Keys.Cast<int>());
        }

        [Fact]
        public void Bulk_delete_confirms_deletes_and_clears_the_selection()
        {
            var items = Row.Many(3);
            var confirmed = new List<string>();
            var cut = RenderSelectable(p => p
                .Add(x => x.Items, items)
                .Add(x => x.OnConfirm, text =>
                {
                    confirmed.Add(text);
                    return Task.FromResult(true);
                })
                .Add(x => x.DeleteItems, _ => Task.FromResult(EditResult.Deleted()))
                .AddChildContent<BulkActions<Row>>(b => b.AddChildContent<BulkDeleteAction<Row>>()));
            Tick(cut, 0);
            Tick(cut, 1);

            cut.Find("button.dv-bulk-action").Click();

            Assert.Equal(["Delete 2 items?"], confirmed);
            Assert.Equal([3], items.Select(r => r.Id));
            Assert.Empty(selectedItems);
            Assert.Single(cut.FindAll("tbody tr"));
        }
    }

    public class DataViewRowActionsTests : TabBlazorTestContext
    {
        private IRenderedComponent<DataView<Row>> RenderWithActions(
            RenderFragment actions,
            Action<ComponentParameterCollectionBuilder<DataView<Row>>>? configure = null)
        {
            return Render<DataView<Row>>(p =>
            {
                p.Add(x => x.Items, Row.Many(2))
                    .Add(x => x.KeySelector, r => r.Id)
                    .Add(x => x.OnConfirm, _ => Task.FromResult(false))
                    .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name))
                    .AddChildContent(actions);
                configure?.Invoke(p);
            });
        }

        private static RenderFragment Actions(int maxVisible, params (string Title, Func<Row, Task>? OnClick)[] actions) => b =>
        {
            b.OpenComponent<RowActions<Row>>(0);
            b.AddComponentParameter(1, nameof(RowActions<Row>.MaxVisible), maxVisible);
            b.AddComponentParameter(2, "ChildContent", (RenderFragment)(c =>
            {
                foreach (var action in actions)
                {
                    c.OpenComponent<RowAction<Row>>(3);
                    c.AddComponentParameter(4, "Title", action.Title);
                    c.AddComponentParameter(5, "OnClick", action.OnClick);
                    c.CloseComponent();
                }
            }));
            b.CloseComponent();
        };

        [Fact]
        public void Edit_and_delete_appear_without_declared_actions()
        {
            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Items, Row.Many(1))
                .Add(x => x.SaveItem, ctx => Task.FromResult(EditResult.Saved(ctx.Draft)))
                .Add(x => x.DeleteItem, _ => Task.FromResult(EditResult.Deleted()))
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name)));

            Assert.Equal(["Edit", "Delete"], cut.FindAll("tbody button.dv-action").Select(b => b.GetAttribute("title")));
        }

        [Fact]
        public void Declared_actions_render_in_order_as_icon_buttons()
        {
            var cut = RenderWithActions(Actions(3, ("Open", null), ("Copy", null)));

            var buttons = cut.Find("tbody tr").QuerySelectorAll("button.dv-action");
            Assert.Equal(["Open", "Copy"], buttons.Select(b => b.GetAttribute("aria-label")));
            Assert.Empty(cut.FindAll("tbody .dv-action-menu"));
        }

        [Fact]
        public void Extra_actions_move_to_the_menu()
        {
            var cut = RenderWithActions(Actions(3, ("A", null), ("B", null), ("C", null), ("D", null)));

            var row = cut.Find("tbody tr");
            Assert.Equal(["A", "B"], row.QuerySelectorAll("button.dv-action").Select(b => b.GetAttribute("aria-label")));
            row.QuerySelector(".dv-action-menu button")!.Click();
            Assert.Equal(["C", "D"], cut.Find("tbody tr").QuerySelectorAll(".dropdown-item").Select(i => i.TextContent.Trim()));
        }

        [Fact]
        public void Click_runs_the_action_with_the_row_and_shows_a_busy_spinner()
        {
            var tcs = new TaskCompletionSource();
            var clicked = new List<int>();
            var cut = RenderWithActions(Actions(3, ("Archive", r =>
            {
                clicked.Add(r.Id);
                return tcs.Task;
            })));

            cut.FindAll("tbody tr")[1].QuerySelector("button.dv-action")!.Click();
            var button = cut.FindAll("tbody tr")[1].QuerySelector("button.dv-action")!;
            Assert.NotNull(button.QuerySelector(".spinner-border"));
            Assert.True(button.HasAttribute("disabled"));

            cut.InvokeAsync(() => tcs.SetResult());
            cut.WaitForAssertion(() => Assert.Null(cut.FindAll("tbody tr")[1].QuerySelector(".spinner-border")));
            Assert.Equal([2], clicked);
        }

        [Fact]
        public void Action_errors_are_shown()
        {
            var cut = RenderWithActions(Actions(3, ("Fail", _ => throw new InvalidOperationException("Locked"))));

            cut.Find("tbody button.dv-action").Click();

            Assert.Contains("Locked", cut.Find(".dv-notice").TextContent);
        }

        [Fact]
        public void Visible_disabled_and_confirm_are_per_row()
        {
            var ran = false;
            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Items, Row.Many(2))
                .Add(x => x.OnConfirm, _ => Task.FromResult(false))
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name))
                .AddChildContent<RowActions<Row>>(a => a
                    .AddChildContent<RowAction<Row>>(x => x.Add(y => y.Title, "Only first").Add(y => y.Visible, r => r.Id == 1))
                    .AddChildContent<RowAction<Row>>(x => x.Add(y => y.Title, "Disabled").Add(y => y.Disabled, _ => true))
                    .AddChildContent<RowAction<Row>>(x => x
                        .Add(y => y.Title, "Confirm")
                        .Add(y => y.Confirm, "Sure?")
                        .Add(y => y.OnClick, _ =>
                        {
                            ran = true;
                            return Task.CompletedTask;
                        }))));

            var rows = cut.FindAll("tbody tr");
            Assert.Equal(3, rows[0].QuerySelectorAll("button.dv-action").Length);
            Assert.Equal(2, rows[1].QuerySelectorAll("button.dv-action").Length);
            Assert.True(rows[1].QuerySelectorAll("button.dv-action")[0].HasAttribute("disabled"));

            rows[1].QuerySelectorAll("button.dv-action")[1].Click();
            Assert.False(ran);
        }

        [Fact]
        public void Href_actions_are_links()
        {
            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Items, Row.Many(1))
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name))
                .AddChildContent<RowActions<Row>>(a => a
                    .AddChildContent<RowAction<Row>>(x => x.Add(y => y.Title, "Open").Add(y => y.Href, r => $"/rows/{r.Id}"))));

            Assert.Equal("/rows/1", cut.Find("tbody a.dv-action").GetAttribute("href"));
        }

        [Fact]
        public void Action_clicks_do_not_click_the_row()
        {
            Row? rowClicked = null;
            var cut = RenderWithActions(Actions(3, ("Go", _ => Task.CompletedTask)),
                p => p.Add(x => x.OnRowClicked, r => rowClicked = r));

            cut.Find("tbody button.dv-action").Click();

            Assert.Null(rowClicked);
        }

        [Fact]
        public void Row_actions_template_gets_the_row_context()
        {
            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Items, Row.Many(1))
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name))
                .AddChildContent<RowActionsTemplate<Row>>(t => t.Add(x => x.ChildContent, ctx => b => b.AddMarkupContent(0, $"<span class=\"mine\">{ctx.Item.Id}/{ctx.IsEditing}</span>"))));

            Assert.Equal("1/False", cut.Find("tbody .mine").TextContent);
        }

        [Fact]
        public async Task Actions_without_parameters_survive_re_renders()
        {
            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Items, Row.Many(1))
                .Add(x => x.SaveItem, ctx => Task.FromResult(EditResult.Saved(ctx.Draft)))
                .Add(x => x.DeleteItem, _ => Task.FromResult(EditResult.Deleted()))
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name))
                .AddChildContent<RowActions<Row>>(a => a
                    .AddChildContent<DeleteAction<Row>>()
                    .AddChildContent<EditAction<Row>>()));

            await cut.InvokeAsync(() => cut.Instance.ReloadAsync());
            cut.Render();

            Assert.Equal(["Delete", "Edit"], cut.FindAll("tbody button.dv-action").Select(b => b.GetAttribute("title")));
        }

        [Fact]
        public void Actions_can_sit_at_the_start()
        {
            var cut = RenderWithActions(Actions(3, ("Go", null)), p => p.Add(x => x.RowActionsPosition, RowActionsPosition.Start));

            Assert.NotNull(cut.Find("tbody tr td:first-child button.dv-action"));
        }
    }

    public class DataViewDetailsTests : TabBlazorTestContext
    {
        private int loads;

        private IRenderedComponent<DataView<Row>> RenderWithDetails(DataDetailsMode mode,
            Action<ComponentParameterCollectionBuilder<DataView<Row>>>? configure = null)
        {
            return Render<DataView<Row>>(p =>
            {
                p.Add(x => x.Items, Row.Many(3))
                    .Add(x => x.KeySelector, r => r.Id)
                    .Add(x => x.DetailsMode, mode)
                    .Add(x => x.LoadDetails, r =>
                    {
                        loads++;
                        return Task.FromResult($"details of {r.Id}");
                    })
                    .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name))
                    .AddChildContent<DetailsTemplate<Row>>(t => t.Add(x => x.ChildContent, ctx => b =>
                        b.AddMarkupContent(0, $"<span class=\"details\">{ctx.Get<string>()}</span>")));
                configure?.Invoke(p);
            });
        }

        [Fact]
        public void Inline_details_expand_under_the_row()
        {
            var cut = RenderWithDetails(DataDetailsMode.Inline);

            cut.FindAll("tbody button.dv-expand")[1].Click();

            var rows = cut.FindAll("tbody tr");
            Assert.Equal(4, rows.Count);
            Assert.Contains("dv-details-row", rows[2].ClassList);
            Assert.Equal("details of 2", rows[2].QuerySelector(".details")!.TextContent);
            Assert.Equal("true", rows[1].QuerySelector("button.dv-expand")!.GetAttribute("aria-expanded"));
        }

        [Fact]
        public void Single_expand_closes_the_previous_details()
        {
            var cut = RenderWithDetails(DataDetailsMode.Inline);

            cut.FindAll("tbody button.dv-expand")[0].Click();
            cut.FindAll("tbody button.dv-expand")[1].Click();

            Assert.Single(cut.FindAll("tr.dv-details-row"));
        }

        [Fact]
        public void Details_are_loaded_once_per_key()
        {
            var cut = RenderWithDetails(DataDetailsMode.Inline);

            cut.FindAll("tbody button.dv-expand")[0].Click();
            cut.FindAll("tbody button.dv-expand")[0].Click();
            cut.FindAll("tbody button.dv-expand")[0].Click();

            Assert.Equal(1, loads);
        }

        [Fact]
        public void Row_click_opens_details()
        {
            var cut = RenderWithDetails(DataDetailsMode.Inline, p => p.Add(x => x.SingleExpand, false));

            cut.FindAll("tbody tr")[0].Click();

            Assert.Single(cut.FindAll("tr.dv-details-row"));
        }

        [Fact]
        public void Drawer_details_follow_the_current_item()
        {
            var cut = RenderWithDetails(DataDetailsMode.Drawer);
            Assert.Empty(cut.FindAll(".offcanvas"));

            cut.FindAll("tbody tr")[0].Click();
            Assert.Equal("Row 01", cut.Find(".offcanvas-title").TextContent.Trim());
            Assert.Equal("details of 1", cut.Find(".offcanvas .details").TextContent);

            cut.FindAll("tbody tr")[2].Click();
            Assert.Equal("details of 3", cut.Find(".offcanvas .details").TextContent);
            Assert.Contains("table-active", cut.FindAll("tbody tr")[2].ClassList);
            Assert.Empty(cut.FindAll("tbody button.dv-expand"));
        }

        [Fact]
        public void Side_panel_shows_a_placeholder_then_the_current_item()
        {
            var cut = RenderWithDetails(DataDetailsMode.SidePanel, p => p.Add(x => x.View, DataViewKind.List));

            Assert.Contains("Select an item", cut.Find(".dv-side-panel").TextContent);

            cut.FindAll(".dv-list-item")[1].Click();

            Assert.Equal("details of 2", cut.Find(".dv-side-panel .details").TextContent);
            cut.Find(".dv-side-panel .btn-close").Click();
            Assert.Empty(cut.FindAll(".dv-side-panel .details"));
        }

        [Fact]
        public void List_view_shows_inline_details_inside_the_item()
        {
            var cut = RenderWithDetails(DataDetailsMode.Inline, p => p.Add(x => x.View, DataViewKind.List));

            cut.FindAll(".dv-list-item")[0].Click();

            Assert.Equal("details of 1", cut.Find(".dv-list-item .dv-list-details .details").TextContent);
        }

        [Fact]
        public void Details_action_toggles_details()
        {
            var cut = RenderWithDetails(DataDetailsMode.Inline, p => p
                .AddChildContent<RowActions<Row>>(a => a.AddChildContent<DetailsAction<Row>>()));

            cut.Find("tbody button.dv-action").Click();

            Assert.Single(cut.FindAll("tr.dv-details-row"));
        }
    }
}
