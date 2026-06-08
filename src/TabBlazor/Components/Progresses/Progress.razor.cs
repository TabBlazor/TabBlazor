using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace TabBlazor
{

    /// <summary>The height of a <see cref="Progress"/> bar.</summary>
    public enum ProgressSize
    {
        /// <summary>Standard height.</summary>
        Default,
        /// <summary>Thin bar.</summary>
        Small,
        /// <summary>Thick bar.</summary>
        Large
    }

    /// <summary>A progress bar supporting a fixed percentage or an indeterminate animation.</summary>
    public partial class Progress : TablerBaseComponent
    {
        /// <summary>The fill color of the bar.</summary>
        [Parameter] public TablerColor Color { get; set; }
        /// <summary>The bar height. Defaults to <see cref="ProgressSize.Default"/>.</summary>
        [Parameter] public ProgressSize Size { get; set; }
        /// <summary>When true, shows an indeterminate animation instead of a fixed value. Defaults to false.</summary>
        [Parameter] public bool Indeterminate { get; set; }
        /// <summary>The fill percentage (0–100).</summary>
        [Parameter] public int Percentage { get; set; }
        /// <summary>Optional text shown on the bar.</summary>
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

