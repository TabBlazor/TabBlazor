using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabBlazor
{
    public partial class TreeView<TItem> : ComponentBase
    {
        [Parameter] public IList<TItem> Items { get; set; }
        [Parameter] public Func<TItem, IList<TItem>> ChildSelector { get; set; } = node => null;

        [Parameter] public RenderFragment<TItem> Template { get; set; }
        [Parameter] public bool ExpandAll { get; set; }
        [Parameter] public Func<TItem, bool> DefaultExpanded { get; set; }
        [Parameter] public bool MultiSelect { get; set; }

        [Parameter] public TItem SelectedItem { get; set; }
        [Parameter] public EventCallback<TItem> SelectedItemChanged { get; set; }

        [Parameter] public List<TItem> SelectedItems { get; set; }
        [Parameter] public EventCallback<List<TItem>> SelectedItemsChanged { get; set; }


        protected bool isExpanded = false;
        private List<TItem> selectedItems = new List<TItem>();
        private List<TItem> expandedItems = new List<TItem>();


        protected override void OnInitialized()
        {
            if (DefaultExpanded != null)
            {
                SetDefaultExpanded(Items);
            }
        }

        private void SetDefaultExpanded(IList<TItem> items)
        {
            foreach (var item in items)
            {
                if (!IsExpanded(item) && DefaultExpanded(item))
                {
                    expandedItems.Add(item);
                }

                SetDefaultExpanded(ChildSelector(item));
            }
        }

        public bool IsSelected(TItem item)
        {
            return selectedItems.Contains(item);
        }

        public bool IsExpanded(TItem item)
        {
            return expandedItems.Contains(item);
        }

        public void ToogleExpanded(TItem item)
        {
            if (IsExpanded(item))
            {
                expandedItems.Remove(item);
            }
            else
            {
                expandedItems.Add(item);
            }
        }

        public async Task ToogleSelected(TItem item)
        {
            bool removed = false;
            if (!MultiSelect)
            {
                selectedItems.Clear();
            }

            if (IsSelected(item))
            {
                selectedItems.Remove(item);
                removed = true;
            }
            else
            {
                selectedItems.Add(item);
            }
           
            if (removed)
            {
                await SelectedItemChanged.InvokeAsync(default);
            }
            else
            {
                await SelectedItemChanged.InvokeAsync(item);
            }


            await SelectedItemsChanged.InvokeAsync(selectedItems);
            StateHasChanged();
        }

    }
}
