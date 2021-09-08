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
        [Parameter] public Func<TItem, IList<TItem>> ChildSelector { get; set; } = node => null;
        [Parameter] public RenderFragment<TItem> Template { get; set; }
        
        [Parameter] public int Level { get; set; }
      
        private bool isRoot => Level == 0;

        protected bool HasChildren(TItem item)
        {
            return @ChildSelector(item).Any();
        }

    }
}
