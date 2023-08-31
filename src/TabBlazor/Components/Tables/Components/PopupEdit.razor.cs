using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;

namespace TabBlazor.Components.Tables.Components;

public partial class PopupEdit<TItem>
{
    [Parameter] public IPopupEditTable<TItem> Table { get; set; }
    [CascadingParameter] public EditContext EditContext { get; set; }

    private TableEditPopupOptions<TItem> popupOptions = new TableEditPopupOptions<TItem>();

    protected override void OnParametersSet()
    {
        popupOptions.Title = Table.IsAddInProgress ? "Add" : "Edit";
        popupOptions.IsAddInProgress = Table.IsAddInProgress;
        popupOptions.ModalOptions = new ModalOptions { Size = ModalSize.Large };
        popupOptions.CurrentEditItem = Table.CurrentEditItem;

        Table.EditPopupMutator?.Invoke(popupOptions);
    }

    private async Task CancelEdit()
    {
        await Table.CancelEdit();
    }
}