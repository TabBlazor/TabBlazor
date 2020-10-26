using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Tabler.Components
{

    public enum TablerProgressSize
    {
        Default,
        Small,
        Large
    }

    public partial class TablerProgress : TablerBaseComponent
    {
        [Parameter] public TablerColor Color { get; set; }
        [Parameter] public TablerProgressSize Size { get; set; }
        [Parameter] public bool Indeterminate { get; set; }
        [Parameter] public int Precentage { get; set; }
        [Parameter] public string Text { get; set; }

        //protected string HtmlTag => "span";
        //.progress-bar-indeterminate
        protected override string ClassNames => ClassBuilder
              .Add("progress")
              .Add(BackgroundColor.GetColorClass("bg", TablerColorType.Default))
              .AddCompare("progress-sm", Size, TablerProgressSize.Small)
              .AddCompare("progress-lg", Size, TablerProgressSize.Large)

              .ToString();


        protected string BarClassNames => ClassBuilder
                 .Add("progress-bar")
                 .Add(Color.GetColorClass("bg"))
                 .AddIf("progress-bar-indeterminate", Indeterminate)
                 .ToString();
    }
}

