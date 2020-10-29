using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tabler.Components
{
   public abstract class TablerDropdownBaseComponent : TablerBaseComponent
    {
        [Parameter] public RenderFragment Dropdown { get; set; }
        [Parameter] public EventCallback<MouseEventArgs> ToogleExpand { get; set; }
        protected bool isExpanded;


        protected void ToogleExpanded()
        {
            isExpanded = !isExpanded;
        }
    }
}
