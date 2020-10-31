using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace Tabler.Components
{
    public partial class Icon : ComponentBase
    {
        [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object> Attributes { get; set; }
        [Parameter] public string Color { get; set; }
        [Parameter] public int Size { get; set; } = 24;
        [Parameter] public double StrokeWidth { get; set; } = 2;
        [Parameter] public string Elements { get; set; }
    }
}
