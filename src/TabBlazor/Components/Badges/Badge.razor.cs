using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace TabBlazor
{
    public enum BadgeShape
    {
        Default,
        Pill
    }

    public partial class Badge : TablerBaseComponent
    {
        [Parameter] public BadgeShape Shape { get; set; }

        protected string HtmlTag => "span";

        protected override string ClassNames => ClassBuilder
              .Add("badge")
              .Add(BackgroundColor.GetColorClass("bg", ColorType.Default))
              .AddCompare("badge-pill", Shape, BadgeShape.Pill)
            .AddIf("cursor-pointer", OnClick.HasDelegate)
              .ToString();
    }


}
