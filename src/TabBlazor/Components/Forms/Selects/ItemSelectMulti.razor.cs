using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabBlazor
{
    public partial class ItemSelectMulti<TItem> : TablerBaseComponent
    {
        [Parameter] public List<TItem> Items { get; set; }
        [Parameter] public string NoSelectedText { get; set; } = "*Select*";
        [Parameter] public string NoItemsText { get; set; }
        [Parameter] public bool ShowCheckBoxes { get; set; }
        [Parameter] public List<TItem> SelectedItems { get; set; } = new List<TItem>();
        [Parameter] public EventCallback<List<TItem>> SelectedItemsChanged { get; set; }
        [Parameter] public Func<TItem, string> SelectedTextExpression { get; set; }
        [Parameter] public RenderFragment<TItem> Template { get; set; }
        [Parameter] public bool Clearable { get; set; }
        [Parameter] public bool Disabled { get; set; }
        [Parameter] public bool RemoveSelectedFromList { get; set; }

        protected List<TItem> selectedItems = new List<TItem>();
        protected Dropdown dropdown;

        protected override void OnInitialized()
        {
            base.OnInitializedAsync();
            selectedItems = SelectedItems;
            if (selectedItems == null) { selectedItems = new List<TItem>(); }

        }

        protected List<TItem> FilteredList()
        {
            if (RemoveSelectedFromList)
            {
                return Items.Where(e => !selectedItems.Contains(e)).ToList();
            }
            return Items;
        }

        private string GetSelectedText(TItem item)
        {
            if (SelectedTextExpression == null) return "No Selected Expresssion Set up!";
            return SelectedTextExpression.Invoke(item);
        }

        private bool IsSelected(TItem item)
        {
            return selectedItems.Contains(item);
        }

        protected async Task RemoveSelected(TItem item)
        {
            if (IsSelected(item))
            {
                selectedItems.Remove(item);
            }
            dropdown.Close();
            await SelectedItemsChanged.InvokeAsync(selectedItems);
        }

        protected async Task ClearSelected()
        {
            selectedItems.Clear();
            dropdown.Close();
            await SelectedItemsChanged.InvokeAsync(selectedItems);
        }

            protected async Task ToogleSelected(TItem item)
        {
            if (IsSelected(item))
            {
                selectedItems.Remove(item);
            }
            else
            {
                selectedItems.Add(item);
            }

            await SelectedItemsChanged.InvokeAsync(selectedItems);
        }

    }
}
