using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabBlazor
{
    /// <summary>
    /// Toggles between two content templates with an optional animation, e.g. for an animated icon switch.
    /// </summary>
    public partial class SwitchContent : TablerBaseComponent
    {
        /// <summary>Whether the active template is shown. Supports two-way binding via <c>@bind-Active</c>.</summary>
        [Parameter] public bool Active { get; set; }
        /// <summary>Raised when the active state changes.</summary>
        [Parameter] public EventCallback<bool> ActiveChanged { get; set; }
        /// <summary>Content shown when not active.</summary>
        [Parameter] public RenderFragment DefaultTemplate { get; set; }
        /// <summary>Content shown when active.</summary>
        [Parameter] public RenderFragment ActiveTemplate { get; set; }

        /// <summary>The transition animation between templates. Defaults to none.</summary>
        [Parameter] public SwitchAnimation Animation { get; set; }

        bool isActive;
        protected override string ClassNames => ClassBuilder
           .Add("switch-icon")
           .AddIf("active", isActive)
           .Add(Animation.GetCssClass())
           .ToString();


        protected override void OnInitialized()
        {
            base.OnInitialized();
            isActive = Active;
        }

        private async Task ToogleActive(MouseEventArgs e)
        {
            isActive = !isActive;
            await ActiveChanged.InvokeAsync(isActive);
            await OnClick.InvokeAsync(e);
        }
    }

    /// <summary>The transition animation used by <see cref="SwitchContent"/>.</summary>
    public enum SwitchAnimation
    {
        /// <summary>No animation.</summary>
        None = 0,
        /// <summary>Fade between templates.</summary>
        Fade = 1,
        /// <summary>Flip between templates.</summary>
        Flip = 2,
        /// <summary>Scale between templates.</summary>
        Scale = 3,
        /// <summary>Slide up.</summary>
        SlideUp = 4,
        /// <summary>Slide left.</summary>
        SlideLeft = 5,
        /// <summary>Slide down.</summary>
        SlideDown = 6,
        /// <summary>Slide right.</summary>
        SlideRight = 7
    }

    public static class SwithExtensions
    {

        public static string GetCssClass(this SwitchAnimation animation)
        {
            return animation switch
            {
                SwitchAnimation.Flip => "switch-icon-flip",
                SwitchAnimation.Fade => "switch-icon-fade",
                SwitchAnimation.Scale => "switch-icon-scale",
                SwitchAnimation.SlideUp => "switch-icon-slide-up",
                SwitchAnimation.SlideLeft => "switch-icon-slide-left",
                SwitchAnimation.SlideDown => "switch-icon-slide-down",
                SwitchAnimation.SlideRight => "switch-icon-slide-right",
                _ => "",
            };
        }

    }
}
