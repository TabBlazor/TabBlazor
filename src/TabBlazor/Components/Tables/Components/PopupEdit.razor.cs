using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using TabBlazor.Components.Modals;
using TabBlazor.Services;

namespace TabBlazor.Components.Tables.Components
{
    public partial class PopupEdit<TItem>
    {
        [Parameter] public IPopupEditTable<TItem> Table { get; set; }

        private ModalOptions options = new ModalOptions { Size = ModalSize.Large };
        private string title => Table.IsAddInProgress ? "Add" : "Edit";

        private async Task CancelEdit()
        {
            await Table.CancelEdit();
        }


    }
}