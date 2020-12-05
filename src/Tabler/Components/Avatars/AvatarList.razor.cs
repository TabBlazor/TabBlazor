using Microsoft.AspNetCore.Components;

namespace TabBlazor.Components
{
    public partial class AvatarList : TablerBaseComponent
    {
        [Parameter] public bool Stacked { get; set; }
        protected override string ClassNames => ClassBuilder
            .Add("avatar-list")
            .AddIf("avatar-list-stacked", Stacked)
           .ToString(); 
    }
}