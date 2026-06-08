using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Drawing;

namespace TabBlazor
{
    /// <summary>
    /// Renders a country flag. Set <see cref="CountryCode"/> (ISO code) or an explicit <see cref="FlagType"/>.
    /// </summary>
    public partial class Flag : TablerBaseComponent
    {
        [Inject] public FlagService FlagService { get; set; }
        /// <summary>The flag size. When null, uses the default size.</summary>
        [Parameter] public FlagSize? Size { get; set; }
        /// <summary>Rotation in degrees.</summary>
        [Parameter] public int Rotate { get; set; }
        /// <summary>The flag to render. Resolved from <see cref="CountryCode"/> when not set.</summary>
        [Parameter] public IFlagType FlagType { get; set; }
        /// <summary>ISO country code used to look up the flag when <see cref="FlagType"/> is not set.</summary>
        [Parameter] public string CountryCode { get; set; }
        /// <summary>Explicit width (CSS value).</summary>
        [Parameter] public string Width  { get; set; }
        /// <summary>Explicit height (CSS value).</summary>
        [Parameter] public string Height { get; set; }

        protected override void OnParametersSet()
        {
            if (FlagType == null && !string.IsNullOrWhiteSpace(CountryCode))
            {
                FlagType = FlagService.GetFlagType(CountryCode);
            }

            base.OnParametersSet();
        }
        protected override string ClassNames => ClassBuilder
            .Add("flag")
            .AddIf("flag-xs", Size == FlagSize.XSmall)
            .AddIf("flag-sm", Size == FlagSize.Small)
            .AddIf("flag-md", Size == FlagSize.Medium)
            .AddIf("flag-lg", Size == FlagSize.Large)
            .AddIf("flag-xl", Size == FlagSize.XLarge)
            .AddIf("flag-2xl", Size == FlagSize.XXLarge)
            .AddIf("cursor-pointer", OnClick.HasDelegate)
            .ToString();

    }



}
