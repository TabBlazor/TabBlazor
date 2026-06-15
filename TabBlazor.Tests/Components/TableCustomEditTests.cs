using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using TabBlazor.Components.Tables;
using TabBlazor.Components.Tables.Components;

namespace TabBlazor.Tests.Components
{
    public class TableCustomEditTests : TabBlazorTestContext
    {
        private sealed class Row
        {
            public string? Name { get; set; }
        }

        private IRenderedComponent<Table<Row>> RenderTable(
            IList<Row> rows,
            Func<Row, Task<ModalResult>> customEdit,
            EventCallback<Row> onEdited = default,
            EventCallback<Row> onAdded = default)
        {
            return Render<Table<Row>>(p =>
            {
                p.Add(t => t.EditMode, TableEditMode.Custom);
                p.Add(t => t.CustomEdit, customEdit);
                p.Add(t => t.Items, rows);
                if (onEdited.HasDelegate) p.Add(t => t.OnItemEdited, onEdited);
                if (onAdded.HasDelegate) p.Add(t => t.OnItemAdded, onAdded);
                p.AddChildContent<Column<Row>>(c => c.Add(x => x.Title, "Name"));
            });
        }

        private EventCallback<Row> Callback(Action<Row> handler) =>
            EventCallback.Factory.Create(this, handler);

        [Fact]
        public async Task Edit_invokes_CustomEdit_and_raises_OnItemEdited_on_ok()
        {
            var row = new Row { Name = "before" };
            Row? editArg = null;
            Row? editedResult = null;

            Task<ModalResult> CustomEdit(Row r)
            {
                editArg = r;
                r.Name = "after";
                return Task.FromResult(ModalResult.Ok(r));
            }

            var cut = RenderTable(new List<Row> { row }, CustomEdit, onEdited: Callback(r => editedResult = r));

            await cut.InvokeAsync(() => cut.Instance.EditItemAsync(row));

            Assert.Same(row, editArg);
            Assert.Same(row, editedResult);
            Assert.Equal("after", row.Name);
        }

        [Fact]
        public async Task Edit_does_not_raise_OnItemEdited_on_cancel()
        {
            var row = new Row { Name = "x" };
            var raised = false;

            Task<ModalResult> CustomEdit(Row r) => Task.FromResult(ModalResult.Cancel());

            var cut = RenderTable(new List<Row> { row }, CustomEdit, onEdited: Callback(_ => raised = true));

            await cut.InvokeAsync(() => cut.Instance.EditItemAsync(row));

            Assert.False(raised);
        }

        [Fact]
        public void Add_button_invokes_CustomEdit_and_raises_OnItemAdded_on_ok()
        {
            var rows = new List<Row>();
            Row? added = null;

            Task<ModalResult> CustomEdit(Row r) => Task.FromResult(ModalResult.Ok(r));

            var cut = RenderTable(rows, CustomEdit, onAdded: Callback(r => { added = r; rows.Add(r); }));

            cut.Find("button.add-row").Click();

            Assert.NotNull(added);
            Assert.Contains(added, rows);
        }
    }
}
