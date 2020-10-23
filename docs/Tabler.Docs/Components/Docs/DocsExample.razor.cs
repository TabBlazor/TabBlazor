using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Tabler.Docs.Components.Docs
{
   public partial class DocsExample : ComponentBase
    {
        [Parameter] public string Title { get; set; }
        [Parameter] public RenderFragment Description { get; set; }
    }
}
