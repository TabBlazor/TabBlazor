using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    public enum AvatarSize
    {
        Default,
        Small,
        Medium,
        Large,
        ExtraLarge
    }

    public enum AvatarRounded
    {
        Default,
        Rounded,
        RoundedSmall,
        RoundedLarge,
        Circle,
        None
    }

    public partial class Avatar : TablerBaseComponent
    {
        [Parameter] public string Data { get; set; } = "";
        [Parameter] public AvatarSize Size { get; set; } = AvatarSize.Default;
        [Parameter] public AvatarRounded Rounded { get; set; } = AvatarRounded.Default;

        protected string Style => string.IsNullOrWhiteSpace(Data) ? string.Empty : $"{GetUnmatchedParameter("style")} background-image:url('{Data}')";
        
        protected override string ClassNames => ClassBuilder
            .Add("avatar")
            .Add(BackgroundColor.GetColorClass("bg", suffix: "lt"))
            .Add(TextColor.GetColorClass("text"))
            .AddCompare(Size, new Dictionary<AvatarSize, string>
            {
                { AvatarSize.Small, "avatar-sm" },
                { AvatarSize.Medium, "avatar-md" },
                { AvatarSize.Large, "avatar-lg" },
                { AvatarSize.ExtraLarge, "avatar-xl" }
            })
            .AddCompare(Rounded, new Dictionary<AvatarRounded, string>
            {
                { AvatarRounded.RoundedSmall, "rounded-sm" },
                { AvatarRounded.Rounded, "rounded" },
                { AvatarRounded.RoundedLarge, "rounded-lg" },
                { AvatarRounded.Circle, "rounded-circle" },
                { AvatarRounded.None, "rounded-0" }
            }).ToString();
    }
}