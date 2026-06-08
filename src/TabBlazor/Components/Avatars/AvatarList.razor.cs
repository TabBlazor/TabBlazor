using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    /// <summary>A container that lays out multiple <see cref="Avatar"/> components in a row.</summary>
    public partial class AvatarList : TablerBaseComponent
    {
        /// <summary>When true, avatars overlap in a stacked layout. Defaults to false.</summary>
        [Parameter] public bool Stacked { get; set; }
        protected override string ClassNames => ClassBuilder
            .Add("avatar-list")
            .AddIf("avatar-list-stacked", Stacked)
           .ToString(); 
    }
}