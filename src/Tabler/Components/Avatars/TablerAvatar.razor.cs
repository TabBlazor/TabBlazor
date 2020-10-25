using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Tabler.Components
{
    public enum TablerAvatarSize
    {
        Default,
        Small,
        Medium,
        Large,
        ExtraLarge
    }

    public enum TablerAvatarRounded
    {
        Default,
        Rounded,
        RoundedSmall,
        RoundedLarge,
        Circle,
        None
    }

    public partial class TablerAvatar : TablerBaseComponent
    {
        [Parameter] public string Url { get; set; } = "";
        [Parameter] public TablerAvatarSize Size { get; set; } = TablerAvatarSize.Default;
        [Parameter] public TablerAvatarRounded Rounded { get; set; } = TablerAvatarRounded.Default;

        protected string Style => $"{GetUnmatchedParameter("style")} background-image:url('{Url}')";
        protected override string ClassNames => ClassBuilder
            .Add("avatar")
            .Add(BackgroundColor.GetColorClass("bg", suffix: "lt"))
            .Add(TextColor.GetColorClass("text"))
            .AddCompare(Size, new Dictionary<TablerAvatarSize, string>
            {
                { TablerAvatarSize.Small, "avatar-sm" },
                { TablerAvatarSize.Medium, "avatar-md" },
                { TablerAvatarSize.Large, "avatar-lg" },
                { TablerAvatarSize.ExtraLarge, "avatar-xl" }
            })
            .AddCompare(Rounded, new Dictionary<TablerAvatarRounded, string>
            {
                { TablerAvatarRounded.RoundedSmall, "rounded-sm" },
                { TablerAvatarRounded.Rounded, "rounded" },
                { TablerAvatarRounded.RoundedLarge, "rounded-lg" },
                { TablerAvatarRounded.Circle, "rounded-circle" },
                { TablerAvatarRounded.None, "rounded-0" }
            }).ToString();
    }
}