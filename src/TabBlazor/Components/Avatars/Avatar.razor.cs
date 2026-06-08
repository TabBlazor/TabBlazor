using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace TabBlazor
{
    /// <summary>The size of an <see cref="Avatar"/>.</summary>
    public enum AvatarSize
    {
        /// <summary>Standard size.</summary>
        Default,
        /// <summary>Small.</summary>
        Small,
        /// <summary>Medium.</summary>
        Medium,
        /// <summary>Large.</summary>
        Large,
        /// <summary>Extra large.</summary>
        ExtraLarge
    }

    /// <summary>The corner rounding of an <see cref="Avatar"/>.</summary>
    public enum AvatarRounded
    {
        /// <summary>Theme default rounding.</summary>
        Default,
        /// <summary>Rounded corners.</summary>
        Rounded,
        /// <summary>Slightly rounded corners.</summary>
        RoundedSmall,
        /// <summary>Largely rounded corners.</summary>
        RoundedLarge,
        /// <summary>Fully circular.</summary>
        Circle,
        /// <summary>Square (no rounding).</summary>
        None
    }

    /// <summary>
    /// A user/entity avatar showing an image or initials. Set <see cref="Data"/> to an image URL, or place
    /// initials in child content. Background/text color come from <see cref="TablerBaseComponent"/>.
    /// </summary>
    public partial class Avatar : TablerBaseComponent
    {
        /// <summary>Image URL shown as the avatar background. When empty, child content (e.g. initials) is shown.</summary>
        [Parameter] public string Data { get; set; } = "";
        /// <summary>The avatar size. Defaults to <see cref="AvatarSize.Default"/>.</summary>
        [Parameter] public AvatarSize Size { get; set; } = AvatarSize.Default;
        /// <summary>The corner rounding. Defaults to <see cref="AvatarRounded.Default"/>.</summary>
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