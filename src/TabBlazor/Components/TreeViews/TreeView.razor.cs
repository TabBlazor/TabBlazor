using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
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
        [Parameter] public bool AlwaysExpanded { get; set; }
        [Parameter] public Func<TItem, bool> DefaultExpanded { get; set; }
        [Parameter] public bool MultiSelect { get; set; }
        [Parameter] public bool AlignTreeNodes { get; set; }

        [Parameter] public TItem SelectedItem { get; set; }
        [Parameter] public EventCallback<TItem> SelectedItemChanged { get; set; }

        [Parameter] public List<TItem> SelectedItems { get; set; }
        [Parameter] public EventCallback<List<TItem>> SelectedItemsChanged { get; set; }

        [Parameter] public List<TItem> CheckedItems { get; set; }
        [Parameter] public EventCallback<List<TItem>> CheckedItemsChanged { get; set; }

        [Parameter] public CheckboxMode CheckboxMode { get; set; }

        [Parameter] public EventCallback<List<TItem>> ExpandedItemsChanged { get; set; }

        protected bool isExpanded = false;
        private List<TItem> selectedItems = new List<TItem>();
        private List<TItem> expandedItems = new List<TItem>();
        private List<TItem> checkedItems = new List<TItem>();


        protected override void OnInitialized()
        {
            if (DefaultExpanded != null)
            {
                SetDefaultExpanded(Items);
            }
        }

        protected override void OnParametersSet()
        {
            if (MultiSelect)
            {
                selectedItems = SelectedItems;
            }
            else
            {
                selectedItems.Clear();
                if (SelectedItem != null)
                {
                    selectedItems.Add(SelectedItem);
                }
            }
        }

        public void ExpandAll()
        {
            ExpandAll(Items);
        }

        public void CollapseAll()
        {
            expandedItems.Clear();
        }

        private void ExpandAll(IList<TItem> items)
        {
            foreach (var item in items)
            {
                if (!IsExpanded(item))
                {
                    expandedItems.Add(item);
                }

                ExpandAll(ChildSelector(item));
            }
        }

        private void CheckAll(IList<TItem> items, bool setChecked)
        {
            foreach (var item in items)
            {
                if (setChecked)
                {
                    if (!checkedItems.Contains(item))
                    {
                        checkedItems.Add(item);
                    }
                }
                else
                {
                    if (checkedItems.Contains(item))
                    {
                        checkedItems.Remove(item);
                    }
                }

                CheckAll(ChildSelector(item), setChecked);
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

        public bool? IsChecked(TItem item)
        {
            return checkedItems.Contains(item);
        }

        public bool IsExpanded(TItem item)
        {
            return expandedItems.Contains(item);
        }

        public async void ToggleExpanded(TItem item)
        {
            if (IsExpanded(item))
            {
                expandedItems.Remove(item);
            }
            else
            {
                expandedItems.Add(item);
            }

            if (ExpandedItemsChanged.HasDelegate)
            {
                await ExpandedItemsChanged.InvokeAsync(expandedItems);
            }
        }

        public async Task ToggleChecked(TItem item)
        {
            if (IsChecked(item) == true)
            {
                checkedItems.Remove(item);
                CheckAll(ChildSelector(item), false);
            }
            else
            {
                checkedItems.Add(item);
                CheckAll(ChildSelector(item), true);
            }

            await CheckedItemsChanged.InvokeAsync(checkedItems);
            StateHasChanged();
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
