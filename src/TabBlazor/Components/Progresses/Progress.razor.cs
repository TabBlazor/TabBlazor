using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace TabBlazor
{

    public enum ProgressSize
    {
        Default,
        Small,
        Large
    }

    public partial class Progress : TablerBaseComponent
    {
        [Parameter] public TablerColor Color { get; set; }
        [Parameter] public ProgressSize Size { get; set; }
        [Parameter] public bool Indeterminate { get; set; }
        [Parameter] public int Precentage { get; set; }
        [Parameter] public string Text { get; set; }

        protected override string ClassNames => ClassBuilder
              .Add("progress")
              .Add(BackgroundColor.GetColorClass("bg", ColorType.Default))
              .AddCompare("progress-sm", Size, ProgressSize.Small)
              .AddCompare("progress-lg", Size, ProgressSize.Large)

              .ToString();


        protected string BarClassNames => ClassBuilder
                 .Add("progress-bar")
                 .Add(Color.GetColorClass("bg"))
                 .AddIf("progress-bar-indeterminate", Indeterminate)
                 .ToString();
    }
}

