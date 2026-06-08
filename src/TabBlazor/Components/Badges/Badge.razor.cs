using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace TabBlazor
{
    /// <summary>The corner style of a <see cref="Badge"/>.</summary>
    public enum BadgeShape
    {
        /// <summary>Standard rounded corners.</summary>
        Default,
        /// <summary>Fully rounded pill shape.</summary>
        Pill
    }

    /// <summary>The size of a <see cref="Badge"/>.</summary>
    public enum BadgeSize
    {
        /// <summary>Standard size.</summary>
        Default,
        /// <summary>Larger badge.</summary>
        Large
    }

    /// <summary>The visual style of a <see cref="Badge"/>.</summary>
    public enum BadgeType
    {
        /// <summary>Solid filled badge.</summary>
        Default,
        /// <summary>Light tinted background.</summary>
        Light,
        /// <summary>Outlined with transparent background.</summary>
        Outline
    }


    /// <summary>
    /// A small count/label badge. Color comes from <see cref="TablerBaseComponent.BackgroundColor"/>.
    /// </summary>
    public partial class Badge : TablerBaseComponent
    {
        /// <summary>The corner style. Defaults to <see cref="BadgeShape.Default"/>.</summary>
        [Parameter] public BadgeShape Shape { get; set; }
        /// <summary>The badge size. Defaults to <see cref="BadgeSize.Default"/>.</summary>
        [Parameter] public BadgeSize Size { get; set; } = BadgeSize.Default;
        /// <summary>The visual style (solid, light, outline). Defaults to <see cref="BadgeType.Default"/>.</summary>
        [Parameter] public BadgeType BadgeType { get; set; } = BadgeType.Default;

        /// <summary>When true, renders as a small notification dot. Defaults to false.</summary>
        [Parameter] public bool Notification { get; set; }
        /// <summary>When true, the badge blinks to draw attention. Defaults to false.</summary>
        [Parameter] public bool Blink { get; set; }

        protected string HtmlTag => "span";



        protected override string ClassNames
        {
            get
            {
                var builder = ClassBuilder;

                builder = builder
                   .Add("badge")
                   .AddCompare("badge-md", Size, BadgeSize.Default)
                   .AddCompare("badge-pill", Shape, BadgeShape.Pill)
                   .AddIf("badge-notification", Notification)
                   .AddIf("badge-blink", Blink)
                   .AddIf("cursor-pointer", OnClick.HasDelegate);
                 


                if (BadgeType == BadgeType.Default)
                {
                    builder = builder
                     .Add(BackgroundColor.GetColorClass("text", ColorType.Default) + "-fg")
                     .Add(BackgroundColor.GetColorClass("bg", ColorType.Default));
                }
                else if (BadgeType == BadgeType.Light)
                {
                    builder = builder
                    .Add(BackgroundColor.GetColorClass("bg", ColorType.Default) + "-lt");
                }
                else if (BadgeType == BadgeType.Outline)
                {
                    builder = builder
                     .Add("badge-outline")
                    .Add(BackgroundColor.GetColorClass("text", ColorType.Default));
              

                }


                    return builder.ToString(); 

            }
        }

    }


}
