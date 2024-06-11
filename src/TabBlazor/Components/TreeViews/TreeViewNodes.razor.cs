using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabBlazor.Components.TreeViews
{
    public partial class TreeViewNodes<TItem> : ComponentBase
    {
        [CascadingParameter(Name = "Root")] private TreeView<TItem> Root { get; set; }
        [Parameter] public IList<TItem> Items { get; set; }
        [Parameter] public Func<TItem, Task<IList<TItem>>> ChildSelectorAsync { get; set; } = node => null;
        [Parameter] public RenderFragment<TItem> Template { get; set; }
        
        [Parameter] public int Level { get; set; }
      
        private bool isRoot => Level == 0;

        private Dictionary<TItem, IList<TItem>> children = new Dictionary<TItem, IList<TItem>>();

        protected override async Task OnParametersSetAsync()
        {
            children.Clear();
            foreach (var item in Items)
            {
                children.TryAdd(item, await ChildSelectorAsync(item));
            }
        }

        protected IList<TItem> GetChildren(TItem item)
        {
            if (!children.TryGetValue(item, out IList<TItem> value)) { return new List<TItem>(); }
            return value;
        }


        protected bool HasChildren(TItem item)
        {
            return GetChildren(item)?.Count > 0;
        }

    }
}
