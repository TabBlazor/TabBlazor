using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    /// <summary>A Bootstrap grid column inside a <see cref="Row"/>, with per-breakpoint width settings.</summary>
    public partial class RowCol : TablerBaseComponent
    {
        /// <summary>Column span at all sizes (1–12). 0 means unset.</summary>
        [Parameter] public int Columns { get; set; } = 0;
        /// <summary>Column span at the xs breakpoint. 0 means unset.</summary>
        [Parameter] public int Xs { get; set; } = 0;
        /// <summary>Column span at the sm breakpoint. 0 means unset.</summary>
        [Parameter] public int Sm { get; set; } = 0;
        /// <summary>Column span at the md breakpoint. 0 means unset.</summary>
        [Parameter] public int Md { get; set; } = 0;
        /// <summary>Column span at the lg breakpoint. 0 means unset.</summary>
        [Parameter] public int Lg { get; set; } = 0;
        /// <summary>Column span at the xl breakpoint. 0 means unset.</summary>
        [Parameter] public int Xl { get; set; } = 0;
        /// <summary>Column span at the xxl breakpoint. 0 means unset.</summary>
        [Parameter] public int XXl { get; set; } = 0;
        /// <summary>When true, the column sizes to its content (<c>col-auto</c>). Defaults to false.</summary>
        [Parameter] public bool Auto { get; set; }

        protected override string ClassNames => ClassBuilder
            //.Add("col")
            .Add(BackgroundColor.GetColorClass("bg"))
            .Add(TextColor.GetColorClass("text"))
            .AddIf($"col-{Columns}", Columns > 0)
            .AddIf($"col-xs-{Xs}", Xs > 0)
            .AddIf($"col-sm-{Sm}", Sm > 0)
            .AddIf($"col-md-{Md}", Md > 0)
            .AddIf($"col-lg-{Lg}", Lg > 0)
            .AddIf($"col-xl-{Xl}", Xl > 0)
            .AddIf($"col-xxl-{XXl}", XXl > 0)
            .AddIf("col-auto", Auto)
            .ToString();
    }
}