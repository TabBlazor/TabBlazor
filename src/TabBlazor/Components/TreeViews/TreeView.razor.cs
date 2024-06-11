namespace TabBlazor
{
    public partial class TreeView<TItem> : ComponentBase
    {
        [Parameter] public IList<TItem> Items { get; set; }
        [Parameter] public Func<TItem, Task<IList<TItem>>> ChildSelectorAsync { get; set; }
        [Parameter] public Func<TItem, IList<TItem>> ChildSelector { get; set; }
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


        protected override async Task OnInitializedAsync()
        {
            SetChildSelector();
            if (DefaultExpanded != null)
            {
                await SetDefaultExpandedAsync(Items);
            }
        }

        protected override void OnParametersSet()
        {
            SetChildSelector();

            checkedItems = CheckedItems ?? new List<TItem>();

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

        private void SetChildSelector()
        {
            if (ChildSelectorAsync == null && ChildSelector == null)
            {
                ChildSelectorAsync = e => null;
            }
            else if (ChildSelectorAsync == null && ChildSelector != null)
            {
                ChildSelectorAsync = e => Task.FromResult(ChildSelector(e));
            }
        }

        public async Task ExpandAllAsync()
        {
            await ExpandAllAsync(Items);
        }

        public void CollapseAll()
        {
            expandedItems.Clear();
        }

        private async Task ExpandAllAsync(IList<TItem> items)
        {
            foreach (var item in items)
            {
                if (!IsExpanded(item))
                {
                    expandedItems.Add(item);
                }

                await ExpandAllAsync(await ChildSelectorAsync(item));
            }
        }

        private async Task CheckAllAsync(IList<TItem> items, bool setChecked)
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

                if (CheckboxMode == CheckboxMode.Recursive)
                {
                    await CheckAllAsync(await ChildSelectorAsync(item), setChecked);
                }
            }
        }


        private async Task SetDefaultExpandedAsync(IList<TItem> items)
        {
            foreach (var item in items)
            {
                if (!IsExpanded(item) && DefaultExpanded(item))
                {
                    expandedItems.Add(item);
                }

                await SetDefaultExpandedAsync(await ChildSelectorAsync(item));
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

        public async void ToggleExpandedAsync(TItem item)
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

        public async Task ToggleCheckedAsync(TItem item)
        {
            if (IsChecked(item) == true)
            {
                checkedItems.Remove(item);
                if (CheckboxMode == CheckboxMode.Recursive)
                {
                    await CheckAllAsync(await ChildSelectorAsync(item), false);
                }

            }
            else
            {
                checkedItems.Add(item);
                if (CheckboxMode == CheckboxMode.Recursive)
                {
                    await CheckAllAsync(await ChildSelectorAsync(item), true);
                }

            }

            await CheckedItemsChanged.InvokeAsync(checkedItems);
            StateHasChanged();
        }


        public async Task ToogleSelectedAsync(TItem item)
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
