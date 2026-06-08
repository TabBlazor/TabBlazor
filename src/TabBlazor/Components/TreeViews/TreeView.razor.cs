using Microsoft.AspNetCore.Components.Web;
using TabBlazor.Components.TreeViews;

namespace TabBlazor
{
    /// <summary>
    /// A hierarchical tree of <typeparamref name="TItem"/> nodes with selection, checkboxes, lazy child
    /// loading, expansion control and optional drag-and-drop. Provide children via <see cref="ChildSelector"/>
    /// or <see cref="ChildSelectorAsync"/>.
    /// </summary>
    public partial class TreeView<TItem> : ComponentBase
    {
        /// <summary>The root items of the tree.</summary>
        [Parameter] public IList<TItem> Items { get; set; }
        /// <summary>Async function returning a node's children. Used for lazy loading.</summary>
        [Parameter] public Func<TItem, Task<IList<TItem>>> ChildSelectorAsync { get; set; }
        /// <summary>Synchronous function returning a node's children.</summary>
        [Parameter] public Func<TItem, IList<TItem>> ChildSelector { get; set; }
        /// <summary>Template rendered for each node.</summary>
        [Parameter] public RenderFragment<TItem> Template { get; set; }
        /// <summary>When true, all nodes stay expanded. Defaults to false.</summary>
        [Parameter] public bool AlwaysExpanded { get; set; }
        /// <summary>Predicate deciding which nodes start expanded.</summary>
        [Parameter] public Func<TItem, bool> DefaultExpanded { get; set; }
        /// <summary>When true, multiple nodes can be selected. Defaults to false.</summary>
        [Parameter] public bool MultiSelect { get; set; }
        /// <summary>When true, aligns nodes regardless of expander presence. Defaults to false.</summary>
        [Parameter] public bool AlignTreeNodes { get; set; }

        /// <summary>The selected node (single-select). Supports <c>@bind-SelectedItem</c>.</summary>
        [Parameter] public TItem SelectedItem { get; set; }
        /// <summary>Raised when the single selection changes.</summary>
        [Parameter] public EventCallback<TItem> SelectedItemChanged { get; set; }

        /// <summary>The selected nodes (multi-select). Supports <c>@bind-SelectedItems</c>.</summary>
        [Parameter] public List<TItem> SelectedItems { get; set; }
        /// <summary>Raised when the multi-selection changes.</summary>
        [Parameter] public EventCallback<List<TItem>> SelectedItemsChanged { get; set; }

        /// <summary>The checked nodes. Supports <c>@bind-CheckedItems</c>.</summary>
        [Parameter] public List<TItem> CheckedItems { get; set; }
        /// <summary>Raised when the checked set changes.</summary>
        [Parameter] public EventCallback<List<TItem>> CheckedItemsChanged { get; set; }

        /// <summary>How checkboxes behave (e.g. recursive checking of children).</summary>
        [Parameter] public CheckboxMode CheckboxMode { get; set; }

        /// <summary>Raised when the set of expanded nodes changes.</summary>
        [Parameter] public EventCallback<List<TItem>> ExpandedItemsChanged { get; set; }

        /// <summary>Raised when a node drag starts.</summary>
        [Parameter] public EventCallback<TItem> ItemDragged { get; set; }
        /// <summary>Raised when a node is dropped onto another.</summary>
        [Parameter] public EventCallback<ItemDropped<TItem>> ItemDropped { get; set; }

        /// <summary>When true, nodes can be reordered via drag-and-drop. Defaults to false.</summary>
        [Parameter] public bool EnableDragAndDrop { get; set; }

        protected bool isExpanded = false;
        private List<TItem> selectedItems = new List<TItem>();
        private List<TItem> expandedItems = new List<TItem>();
        private List<TItem> checkedItems = new List<TItem>();

        public TItem DraggedItem;

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

        internal async Task SetDraggedAsync(TItem item)
        {
            DraggedItem = item;
            await ItemDragged.InvokeAsync(item);
        }

        internal async Task SetDroppedAsync(TItem targetItem, DragEventArgs e)
        {
          
            if (DraggedItem != null && targetItem != null && !targetItem.Equals(DraggedItem))
            {
                await ItemDropped.InvokeAsync(new ItemDropped<TItem> { Item = DraggedItem, TargetItem = targetItem, DragEventArgs = e });
            }
            DraggedItem = default;

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

        /// <summary>Expands every node in the tree.</summary>
        public async Task ExpandAllAsync()
        {
            await ExpandAllAsync(Items);
        }

        /// <summary>Collapses every node in the tree.</summary>
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

        /// <summary>Returns whether the given node is selected.</summary>
        public bool IsSelected(TItem item)
        {
            return selectedItems.Contains(item);
        }

        /// <summary>Returns whether the given node is checked.</summary>
        public bool? IsChecked(TItem item)
        {
            return checkedItems.Contains(item);
        }

        /// <summary>Returns whether the given node is expanded.</summary>
        public bool IsExpanded(TItem item)
        {
            return expandedItems.Contains(item);
        }

        /// <summary>Toggles the expanded state of the given node.</summary>
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

        /// <summary>Toggles the checked state of the given node (and children when recursive).</summary>
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


        /// <summary>Toggles the selection of the given node, respecting <see cref="MultiSelect"/>.</summary>
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
