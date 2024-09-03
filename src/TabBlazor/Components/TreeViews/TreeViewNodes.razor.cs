using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TabBlazor.Components.TreeViews
{
    public partial class TreeViewNodes<TItem> : ComponentBase
    {
        [CascadingParameter(Name = "Root")] private TreeView<TItem> Root { get; set; }
        [Parameter] public IList<TItem> Items { get; set; }
        [Parameter] public Func<TItem, Task<IList<TItem>>> ChildSelectorAsync { get; set; } = node => null;
        [Parameter] public RenderFragment<TItem> Template { get; set; }

        [Parameter] public int Level { get; set; }

        [Parameter] public bool AllowDrop { get; set; }

        private bool CheckAllowDrop(TItem item)
        {
            if (!AllowDrop) { return false; }

            if (Root.DraggedItem == null || Root.DraggedItem.Equals(item)) { return false; }

            return true;
        }

        private bool isRoot => Level == 0;

        private Dictionary<TItem, IList<TItem>> children = new Dictionary<TItem, IList<TItem>>();

        protected override async Task OnParametersSetAsync()
        {
            children.Clear();
            foreach (var item in Items)
            {
                children.TryAdd(item, await ChildSelectorAsync(item));
            }

            StateHasChanged();
        }

        protected IList<TItem> GetChildren(TItem item)
        {
            if (!children.TryGetValue(item, out IList<TItem> value)) { return new List<TItem>(); }
            return value;
        }

        private string GetNodeCss(TItem item)
        {
            var result = "";

            if (Root.DraggedItem != null)
            {
                if (!CheckAllowDrop(item))
                {
                    result += "tree-node-no-drop ";
                }
               
            }

            return result;
        }

        private string GetContainerCss(TItem item)
        {
            return "d-flex align-items-center mb-2 tree-container ";
        }


        protected bool HasChildren(TItem item)
        {
            return GetChildren(item)?.Count > 0;
        }

        private async Task DragStart(DragEventArgs e, TItem item)
        {
            if (Root.DraggedItem != null)
            {
                return;
            }
            await Root.SetDraggedAsync(item);
        }

        private async Task DragEnd(DragEventArgs e, TItem item)
        {
            await Root.SetDroppedAsync(default);
        }

        private async Task OnDrop(DragEventArgs e, TItem item)
        {
            if (AllowDrop)
            {
                await Root.SetDroppedAsync(item);
            }


        }
    }
}
