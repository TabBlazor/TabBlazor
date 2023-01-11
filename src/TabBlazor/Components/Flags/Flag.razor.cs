using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Drawing;

namespace TabBlazor
{
    public partial class Flag : TablerBaseComponent
    {
        [Inject] public FlagService FlagService { get; set; }
        [Parameter] public FlagSize? Size { get; set; }
        [Parameter] public int Rotate { get; set; }
        [Parameter] public IFlagType FlagType { get; set; }
        [Parameter] public string CountryCode { get; set; }
        [Parameter] public string Width  { get; set; }
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
