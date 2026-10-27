#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace TabBlazor.Tests.Components.DataViews
{
    public class DataViewEditingTests : TabBlazorTestContext
    {
        private readonly List<string> confirmations = [];
        private bool confirmAnswer = true;
        private readonly List<DataEditContext<Row>> saves = [];
        private Func<DataEditContext<Row>, EditResult<Row>> saveResult = ctx => EditResult.Saved(ctx.Draft);

        private Task<bool> Confirm(string text)
        {
            confirmations.Add(text);
            return Task.FromResult(confirmAnswer);
        }

        private Task<EditResult<Row>> Save(DataEditContext<Row> ctx)
        {
            saves.Add(ctx);
            return Task.FromResult(saveResult(ctx));
        }

        private IRenderedComponent<DataView<Row>> RenderEditable(
            IEnumerable<Row> items,
            Action<ComponentParameterCollectionBuilder<DataView<Row>>>? configure = null)
        {
            return Render<DataView<Row>>(p =>
            {
                p.Add(x => x.Items, items)
                    .Add(x => x.KeySelector, r => r.Id)
                    .Add(x => x.SaveItem, Save)
                    .Add(x => x.OnConfirm, Confirm)
                    .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name).Add(x => x.Sortable, true).Add(x => x.Searchable, true))
                    .AddChildContent<DataColumn<Row, decimal>>(c => c.Add(x => x.Value, r => r.Total))
                    .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name + "!").Add(x => x.Id, "shout"));
                configure?.Invoke(p);
            });
        }

        private static void StartEditing(IRenderedComponent<DataView<Row>> cut, int row = 0) =>
            cut.FindAll("tbody tr")[row].QuerySelector("button.dv-action")!.Click();

        [Fact]
        public void Edit_action_turns_the_row_into_editors()
        {
            var cut = RenderEditable(Row.Many(2));

            StartEditing(cut);

            var row = cut.Find("tr.dv-edit-row");
            Assert.Equal("Row 01", row.QuerySelector("input.form-control")!.GetAttribute("value"));
            Assert.Equal("number", row.QuerySelectorAll("input")[1].GetAttribute("type"));
            Assert.NotNull(row.QuerySelector("button.dv-save"));
            Assert.Equal(1, cut.FindAll("tr.dv-edit-row").Count);
        }

        [Fact]
        public void Calculated_columns_are_read_only_while_editing()
        {
            var cut = RenderEditable(Row.Many(1));

            StartEditing(cut);

            var cells = cut.Find("tr.dv-edit-row").QuerySelectorAll("td");
            Assert.Null(cells[2].QuerySelector("input"));
            Assert.Equal("Row 01!", cells[2].TextContent.Trim());
        }

        [Fact]
        public void Cancel_throws_the_draft_away()
        {
            var items = Row.Many(1);
            Row? canceled = null;
            var cut = RenderEditable(items, p => p.Add(x => x.OnEditCanceled, r => canceled = r));
            StartEditing(cut);

            cut.Find("tr.dv-edit-row input.form-control").Change("Changed");
            confirmAnswer = true;
            cut.Find("button.dv-cancel").Click();

            Assert.Equal("Row 01", items[0].Name);
            Assert.Empty(cut.FindAll("tr.dv-edit-row"));
            Assert.Same(items[0], canceled);
            Assert.Equal(["Discard changes?"], confirmations);
        }

        [Fact]
        public void Untouched_draft_cancels_without_asking()
        {
            var cut = RenderEditable(Row.Many(1));
            StartEditing(cut);

            cut.Find("button.dv-cancel").Click();

            Assert.Empty(confirmations);
            Assert.Empty(cut.FindAll("tr.dv-edit-row"));
        }

        [Fact]
        public void Save_replaces_the_original_with_the_saved_item()
        {
            var items = Row.Many(2);
            var original = items[0];
            Row? saved = null;
            var cut = RenderEditable(items, p => p.Add(x => x.OnSaved, r => saved = r));
            StartEditing(cut);

            cut.Find("tr.dv-edit-row input.form-control").Change("Renamed");
            cut.Find("button.dv-save").Click();

            var ctx = Assert.Single(saves);
            Assert.Same(original, ctx.Original);
            Assert.Equal("Row 01", original.Name);
            Assert.Equal("Renamed", ctx.Draft.Name);
            Assert.Same(ctx.Draft, items[0]);
            Assert.Same(ctx.Draft, saved);
            Assert.Empty(cut.FindAll("tr.dv-edit-row"));
            Assert.Equal("Renamed", cut.Find("tbody tr td").TextContent.Trim());
            Assert.Contains("dv-row-saved", cut.Find("tbody tr").ClassList);
        }

        [Fact]
        public void Enter_saves_and_escape_cancels()
        {
            var cut = RenderEditable(Row.Many(1));
            StartEditing(cut);

            cut.Find("tr.dv-edit-row").KeyDown(new KeyboardEventArgs { Key = "Enter" });
            cut.Find("tr.dv-edit-row").KeyUp(new KeyboardEventArgs { Key = "Enter" });
            Assert.Single(saves);

            StartEditing(cut);
            cut.Find("tr.dv-edit-row").KeyDown(new KeyboardEventArgs { Key = "Escape" });
            Assert.Empty(cut.FindAll("tr.dv-edit-row"));
        }

        [Fact]
        public void Enter_saves_the_value_changed_while_the_key_was_down()
        {
            var cut = RenderEditable(Row.Many(1));
            StartEditing(cut);

            cut.Find("tr.dv-edit-row").KeyDown(new KeyboardEventArgs { Key = "Enter" });
            Assert.Empty(saves);
            cut.Find("tr.dv-edit-row input.form-control").Change("Typed");
            cut.Find("tr.dv-edit-row").KeyUp(new KeyboardEventArgs { Key = "Enter" });

            Assert.Equal("Typed", Assert.Single(saves).Draft.Name);
        }

        [Fact]
        public void Enter_released_without_being_pressed_in_the_row_does_not_save()
        {
            var cut = RenderEditable(Row.Many(1));
            StartEditing(cut);

            cut.Find("tr.dv-edit-row").KeyUp(new KeyboardEventArgs { Key = "Enter" });

            Assert.Empty(saves);
        }

        [Fact]
        public void Canceling_an_edit_of_a_nested_value_leaves_the_row_unchanged()
        {
            var shipment = new Shipment { Id = 1, To = new Address { City = "Oslo" } };
            var cut = Render<DataView<Shipment>>(p => p
                .Add(x => x.Items, new List<Shipment> { shipment })
                .Add(x => x.KeySelector, s => s.Id)
                .Add(x => x.SaveItem, ctx => Task.FromResult(EditResult.Saved(ctx.Draft)))
                .Add(x => x.OnConfirm, _ => Task.FromResult(true))
                .AddChildContent<DataColumn<Shipment, string>>(c => c.Add(x => x.Value, s => s.To!.City)));
            var address = shipment.To;
            cut.Find("tbody tr button.dv-action").Click();

            cut.Find("tr.dv-edit-row input.form-control").Change("Bergen");
            cut.Find("button.dv-cancel").Click();

            Assert.Same(address, shipment.To);
            Assert.Equal("Oslo", shipment.To!.City);
            Assert.Equal("Oslo", cut.Find("tbody tr td").TextContent.Trim());
        }

        [Fact]
        public void Deleting_a_selected_row_updates_the_bound_selection()
        {
            IReadOnlyList<Row> selected = [];
            var cut = RenderEditable(Row.Many(2), p => p
                .Add(x => x.SelectionMode, DataSelectionMode.Multiple)
                .Add(x => x.SelectedItemsChanged, s => selected = s)
                .Add(x => x.DeleteItem, _ => Task.FromResult(EditResult.Deleted())));
            cut.FindAll("tbody tr")[0].QuerySelector("input.dv-select")!.Change(true);
            Assert.Single(selected);

            cut.FindAll("tbody tr")[0].QuerySelectorAll("button.dv-action")[1].Click();

            Assert.Empty(selected);
        }

        [Fact]
        public void Failed_save_shows_the_message_and_keeps_the_draft()
        {
            saveResult = _ => EditResult.Failed("Order is locked");
            var items = Row.Many(1);
            var cut = RenderEditable(items);
            StartEditing(cut);
            cut.Find("tr.dv-edit-row input.form-control").Change("Renamed");

            cut.Find("button.dv-save").Click();

            Assert.Contains("Order is locked", cut.Find("tr.dv-edit-message").TextContent);
            Assert.Equal("Renamed", cut.Find("tr.dv-edit-row input.form-control").GetAttribute("value"));
            Assert.Equal("Row 01", items[0].Name);
        }

        [Fact]
        public void Invalid_save_shows_server_errors_on_the_fields()
        {
            saveResult = _ => EditResult.Invalid(("name", "Name already exists"));
            var cut = RenderEditable(Row.Many(1));
            StartEditing(cut);

            cut.Find("button.dv-save").Click();

            var cell = cut.Find("tr.dv-edit-row td");
            Assert.Contains("Name already exists", cell.TextContent);
            Assert.Contains("is-invalid", cell.QuerySelector("input")!.ClassList);
        }

        [Fact]
        public void Exceptions_while_saving_show_a_generic_error()
        {
            saveResult = _ => throw new InvalidOperationException("db down");
            var cut = RenderEditable(Row.Many(1));
            StartEditing(cut);

            cut.Find("button.dv-save").Click();

            Assert.Contains("Could not save", cut.Find("tr.dv-edit-message").TextContent);
            Assert.DoesNotContain("db down", cut.Markup);
        }

        [Fact]
        public void Data_annotations_are_validated_before_saving()
        {
            var cut = RenderEditable(Row.Many(1));
            StartEditing(cut);

            cut.Find("tr.dv-edit-row input.form-control").Change("");
            cut.Find("button.dv-save").Click();

            Assert.Empty(saves);
            Assert.Contains("required", cut.Find("tr.dv-edit-row td").TextContent, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Changing_the_sort_while_editing_asks_to_discard_changes()
        {
            var cut = RenderEditable(Row.Many(2));
            StartEditing(cut);
            cut.Find("tr.dv-edit-row input.form-control").Change("Renamed");

            confirmAnswer = false;
            cut.Find("thead button.dv-sort-button").Click();
            Assert.NotNull(cut.Find("tr.dv-edit-row"));
            Assert.Equal("none", cut.Find("thead th").GetAttribute("aria-sort"));

            confirmAnswer = true;
            cut.Find("thead button.dv-sort-button").Click();
            Assert.Empty(cut.FindAll("tr.dv-edit-row"));
            Assert.Equal("ascending", cut.Find("thead th").GetAttribute("aria-sort"));
        }

        [Fact]
        public void Editing_locks_navigation_while_the_draft_has_changes()
        {
            var cut = RenderEditable(Row.Many(1));
            Assert.Empty(cut.FindComponents<Microsoft.AspNetCore.Components.Routing.NavigationLock>());

            StartEditing(cut);
            cut.Find("tr.dv-edit-row input.form-control").Change("Changed");

            var navigationLock = cut.FindComponent<Microsoft.AspNetCore.Components.Routing.NavigationLock>();
            Assert.True(navigationLock.Instance.ConfirmExternalNavigation);
        }

        [Fact]
        public void Edit_starting_can_cancel_the_edit()
        {
            var cut = RenderEditable(Row.Many(1), p => p.Add(x => x.OnEditStarting, args => args.Cancel = args.Item!.Id == 1));

            StartEditing(cut);

            Assert.Empty(cut.FindAll("tr.dv-edit-row"));
        }

        [Fact]
        public void Allow_edit_hides_the_edit_action_per_row()
        {
            var cut = RenderEditable(Row.Many(2), p => p.Add(x => x.AllowEdit, r => r.Id == 2));

            var rows = cut.FindAll("tbody tr");
            Assert.Null(rows[0].QuerySelector("button.dv-action"));
            Assert.NotNull(rows[1].QuerySelector("button.dv-action"));
        }

        [Fact]
        public void Edit_model_factory_creates_the_draft()
        {
            var cut = RenderEditable(Row.Many(1), p => p.Add(x => x.EditModelFactory, r => new Row { Id = r.Id, Name = r.Name + " (copy)" }));

            StartEditing(cut);

            Assert.Equal("Row 01 (copy)", cut.Find("tr.dv-edit-row input.form-control").GetAttribute("value"));
        }

        [Fact]
        public void Add_shows_an_empty_row_on_top_and_inserts_the_saved_item()
        {
            var items = Row.Many(2);
            var cut = RenderEditable(items, p => p.Add(x => x.NewItem, () => new Row { Id = 99 }));

            cut.Find("button.dv-add").Click();
            var editRow = cut.Find("tbody tr");
            Assert.Contains("dv-edit-row", editRow.ClassList);
            Assert.True(cut.Find("button.dv-add").HasAttribute("disabled"));

            editRow.QuerySelector("input.form-control")!.Change("New one");
            cut.Find("button.dv-save").Click();

            Assert.True(saves.Single().IsNew);
            Assert.Equal(3, items.Count);
            Assert.Contains(cut.FindAll("tbody td"), td => td.TextContent.Trim() == "New one");
        }

        [Fact]
        public async Task Saved_item_hidden_by_search_shows_a_notice()
        {
            var items = Row.Many(2);
            var cut = RenderEditable(items, p => p.Add(x => x.NewItem, () => new Row { Id = 99 }).Add(x => x.SearchDelay, 0));
            await cut.InvokeAsync(() => cut.Instance.SetSearchAsync("Row"));

            cut.Find("button.dv-add").Click();
            cut.Find("tr.dv-edit-row input.form-control").Change("Other");
            cut.Find("button.dv-save").Click();

            Assert.Contains("not shown", cut.Find(".dv-notice").TextContent);
        }

        [Fact]
        public void Popup_mode_edits_in_a_modal_form()
        {
            var items = Row.Many(1);
            var cut = RenderEditable(items, p => p.Add(x => x.EditMode, DataEditMode.Popup));

            StartEditing(cut);

            var form = cut.Find(".modal form.dv-edit-form");
            Assert.Equal(["Name", "Total"], form.QuerySelectorAll("label.form-label").Select(l => l.TextContent).Take(2));
            form.QuerySelector("input.form-control")!.Change("From popup");
            form.Submit();

            Assert.Equal("From popup", items[0].Name);
            Assert.Empty(cut.FindAll(".modal"));
        }

        [Fact]
        public void Popup_close_button_asks_before_discarding()
        {
            var cut = RenderEditable(Row.Many(1), p => p.Add(x => x.EditMode, DataEditMode.Popup));
            StartEditing(cut);
            cut.Find(".modal input.form-control").Change("Changed");

            confirmAnswer = false;
            cut.Find(".modal .btn-close").Click();

            Assert.NotNull(cut.Find(".modal form"));
        }

        [Fact]
        public void List_view_edits_in_a_popup()
        {
            var cut = RenderEditable(Row.Many(1), p => p.Add(x => x.View, DataViewKind.List));

            cut.Find(".dv-list-actions button").Click();
            cut.FindAll(".dv-list-actions .dropdown-item")[0].Click();

            Assert.NotNull(cut.Find(".modal form.dv-edit-form"));
        }

        [Fact]
        public void Drawer_mode_edits_in_an_offcanvas()
        {
            var cut = RenderEditable(Row.Many(1), p => p.Add(x => x.EditMode, DataEditMode.Drawer));

            StartEditing(cut);

            Assert.NotNull(cut.Find(".offcanvas form.dv-edit-form"));
            Assert.Equal("Edit", cut.Find(".offcanvas-title").TextContent.Trim());
        }

        [Fact]
        public async Task Custom_mode_hands_the_context_to_your_own_ui()
        {
            DataEditContext<Row>? custom = null;
            var items = Row.Many(1);
            var cut = RenderEditable(items, p => p
                .Add(x => x.EditMode, DataEditMode.Custom)
                .Add(x => x.OnCustomEdit, ctx =>
                {
                    custom = ctx;
                    return Task.CompletedTask;
                }));

            StartEditing(cut);
            Assert.NotNull(custom);
            Assert.Empty(cut.FindAll("tr.dv-edit-row"));

            custom!.Draft.Name = "Custom";
            Assert.True(await cut.InvokeAsync(() => custom.SaveAsync()));
            Assert.Equal("Custom", items[0].Name);
        }

        [Fact]
        public void Default_editors_follow_the_value_type()
        {
            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Items, Row.Many(1))
                .Add(x => x.SaveItem, Save)
                .Add(x => x.EditMode, DataEditMode.Popup)
                .AddChildContent<DataColumn<Row, bool>>(c => c.Add(x => x.Value, r => r.Active))
                .AddChildContent<DataColumn<Row, SourceTestStatus>>(c => c.Add(x => x.Value, r => r.Status))
                .AddChildContent<DataColumn<Row, DateTime>>(c => c.Add(x => x.Value, r => r.Date))
                .AddChildContent<DataColumn<Row, int>>(c => c.Add(x => x.Value, r => r.Id).Add(x => x.ReadOnly, true)));

            cut.Find("button.dv-action").Click();

            var fields = cut.FindAll(".dv-field");
            Assert.Equal("checkbox", fields[0].QuerySelector("input")!.GetAttribute("type"));
            Assert.Equal(["Open", "On hold", "Closed"], fields[1].QuerySelectorAll("select option").Select(o => o.TextContent));
            Assert.Equal("date", fields[2].QuerySelector("input")!.GetAttribute("type"));
            Assert.Null(fields[3].QuerySelector("input"));
        }

        [Fact]
        public void Editor_template_replaces_the_default_editor()
        {
            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Items, Row.Many(1))
                .Add(x => x.SaveItem, Save)
                .AddChildContent<DataColumn<Row, string>>(c => c
                    .Add(x => x.Value, r => r.Name)
                    .Add(x => x.EditorTemplate, r => b => b.AddMarkupContent(0, $"<span class=\"custom-editor\">{r.Name}</span>"))));

            cut.Find("button.dv-action").Click();

            Assert.Equal("Row 01", cut.Find("tr.dv-edit-row .custom-editor").TextContent);
        }

        // Delete

        [Fact]
        public void Delete_asks_then_removes_the_item()
        {
            var items = Row.Many(2);
            Row? deleted = null;
            var cut = RenderEditable(items, p => p.Add(x => x.DeleteItem, r =>
            {
                deleted = r;
                return Task.FromResult(EditResult.Deleted());
            }));

            cut.FindAll("tbody tr")[0].QuerySelectorAll("button.dv-action")[1].Click();

            Assert.Equal(["Delete this item?"], confirmations);
            Assert.Equal(1, deleted!.Id);
            Assert.Single(items);
            Assert.Single(cut.FindAll("tbody tr"));
        }

        [Fact]
        public void Declined_delete_does_nothing()
        {
            var items = Row.Many(2);
            var called = false;
            confirmAnswer = false;
            var cut = RenderEditable(items, p => p.Add(x => x.DeleteItem, _ =>
            {
                called = true;
                return Task.FromResult(EditResult.Deleted());
            }));

            cut.FindAll("tbody tr")[0].QuerySelectorAll("button.dv-action")[1].Click();

            Assert.False(called);
            Assert.Equal(2, items.Count);
        }

        [Fact]
        public void Failed_delete_shows_a_notice()
        {
            var cut = RenderEditable(Row.Many(1), p => p.Add(x => x.DeleteItem, _ => Task.FromResult(EditResult.Failed("Still in use"))));

            cut.Find("tbody tr").QuerySelectorAll("button.dv-action")[1].Click();

            Assert.Contains("Still in use", cut.Find(".dv-notice").TextContent);
            cut.Find(".dv-notice .btn-close").Click();
            Assert.Empty(cut.FindAll(".dv-notice"));
        }

        // Remote sources

        [Fact]
        public void Saving_with_a_remote_source_replaces_the_row_without_reloading()
        {
            var source = FakeSource<Row>.Answering(Row.Many(3));
            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Source, source)
                .Add(x => x.KeySelector, r => r.Id)
                .Add(x => x.SaveItem, ctx => Task.FromResult(EditResult.Saved(new Row { Id = ctx.Draft.Id, Name = "From server" })))
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name)));
            var calls = source.Calls.Count;

            cut.Find("button.dv-action").Click();
            cut.Find("button.dv-save").Click();

            Assert.Equal(calls, source.Calls.Count);
            Assert.Equal("From server", cut.Find("tbody td").TextContent.Trim());
        }
    }
}
