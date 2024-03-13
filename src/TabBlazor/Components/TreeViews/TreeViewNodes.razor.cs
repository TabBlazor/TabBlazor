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
        [Parameter] public Func<TItem, Task<IList<TItem>>> ChildSelector { get; set; } = node => null;
        [Parameter] public RenderFragment<TItem> Template { get; set; }
        
        [Parameter] public int Level { get; set; }
      
        private bool isRoot => Level == 0;

        private Dictionary<TItem, IList<TItem>> children = new Dictionary<TItem, IList<TItem>>();

        protected override async Task OnParametersSetAsync()
        {
            children.Clear();
            foreach (var item in Items)
            {
                children.Add(item, await ChildSelector(item));
            }
        }

        protected IList<TItem> GetChildren(TItem item)
        {
            return children[item];
        }


        protected bool HasChildren(TItem item)
        {
            return children[item]?.Count > 0;
        }

    }
}
