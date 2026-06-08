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
    /// <summary>Renders one level of nodes within a <see cref="TreeView{TItem}"/>. Used internally and recursively.</summary>
    public partial class TreeViewNodes<TItem> : ComponentBase
    {
        [CascadingParameter(Name = "Root")] private TreeView<TItem> Root { get; set; }
        /// <summary>The nodes to render at this level.</summary>
        [Parameter] public IList<TItem> Items { get; set; }
        /// <summary>Function returning a node's children.</summary>
        [Parameter] public Func<TItem, Task<IList<TItem>>> ChildSelectorAsync { get; set; } = node => null;
        /// <summary>Template rendered for each node.</summary>
        [Parameter] public RenderFragment<TItem> Template { get; set; }

        /// <summary>The nesting depth of this level (0 = root).</summary>
        [Parameter] public int Level { get; set; }

        /// <summary>When true, nodes at this level accept drops. Defaults to false.</summary>
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
            await Root.SetDroppedAsync(default, e);
        }

        private async Task OnDrop(DragEventArgs e, TItem item)
        {
            if (AllowDrop)
            {
                await Root.SetDroppedAsync(item, e);
            }


        }
    }
}
