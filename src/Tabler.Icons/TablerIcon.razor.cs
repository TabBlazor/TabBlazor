using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Tabler.Icons
{
    public partial class TablerIcon : ComponentBase
    {
        [Inject] public IIconService IconService { get; set; }
        [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object> Attributes { get; set; }
        [Parameter] public TablerIconType IconType { get; set; }
        [Parameter] public string Color { get; set; }
        [Parameter] public int Size { get; set; } = 24;
        [Parameter] public double StrokeWidth { get; set; } = 2;

        private TablerIconType? iconType;
        private string svgElements;

        protected override async Task OnParametersSetAsync()
        {
            if (iconType != IconType || string.IsNullOrWhiteSpace(svgElements))
            {
                svgElements = await IconService.GetIcon(IconType.GetIconName());
                iconType = IconType;
            }
        }
    }
}
