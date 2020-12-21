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
        [Parameter] public RenderFragment<TItem> Template { get; set;}
        [Parameter] public bool ExpandAll { get; set; } = true;
        [Parameter] public bool IsRoot { get; set; } = true;

        protected bool isExpanded = false;

        protected override void OnInitialized()
        {
            SetExpanded();
        }
        protected void SetExpanded()
        {
            if (ExpandAll)
            {
                isExpanded = true;
            }
        }

        protected bool HasChildren(TItem item)
        {
            return @ChildSelector(item).Any();
        }

        protected void ToogleExpanded()
        {
            isExpanded = !isExpanded;
        }

    }
}
