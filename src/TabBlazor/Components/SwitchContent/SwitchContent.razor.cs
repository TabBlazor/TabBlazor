using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabBlazor
{
    public partial class SwitchContent : TablerBaseComponent
    {
        [Parameter] public bool Active { get; set; }
        [Parameter] public EventCallback<bool> ActiveChanged { get; set; }
        [Parameter] public RenderFragment DefaultTemplate { get; set; }
        [Parameter] public RenderFragment ActiveTemplate { get; set; }
        
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

    public enum SwitchAnimation
    {
        None = 0,
        Fade = 1,
        Flip = 2,
        Scale = 3,
        SlideUp = 4,
        SlideLeft = 5,
        SlideDown = 6,
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
