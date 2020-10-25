using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Tabler.Components
{
    public enum TablerBadgeShape
    {
        Default,
        Pill
    }

    public partial class TablerBadge : TablerBaseComponent
    {
        [Parameter] public TablerBadgeShape Shape { get; set; }

        protected string HtmlTag => "span";

        protected override string ClassNames => ClassBuilder
              .Add("badge")
              .Add(BackgroundColor.GetColorClass("bg", TablerColorType.Default))
              .AddCompare("badge-pill", Shape, TablerBadgeShape.Pill)
              .ToString();
    }


}
