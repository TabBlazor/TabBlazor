using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TabBlazor.Services;

namespace TabBlazor.Components.Toasts
{
    public partial class ToastView : IDisposable
    {
        [Inject] ToastService ToastService { get;set;}
        [Parameter] public ToastModel Toast { get; set; }

        private string PositionClassNames => GetPositionClassNames();
        private CountdownTimer _countdownTimer;
        private int _progress = 100;

        protected override void OnInitialized()
        {
            if (Toast.Options.AutoClose)
            {
                _countdownTimer = new CountdownTimer(Toast.Options.Delay * 1000);
                _countdownTimer.OnTick += CalculateProgress;
                _countdownTimer.Start();
            } 
        }

        private async void CalculateProgress(int percentComplete)
        {
            _progress = 100 - percentComplete;
            if (percentComplete >= 100)
            {
                await Close();
            }
            await InvokeAsync(StateHasChanged);
        }

        private string GetPositionClassNames()
        {
            return Toast.Options.Position switch {
                ToastPosition.TopRight => "end-0",
                ToastPosition.BottomRight => "end-0 bottom-0",
                ToastPosition.BottomLeft => "bottom-0",
                ToastPosition.TopLeft => "start-0",
                _ => "end-0"
            };
        }

        public async Task Close()
        {
            await ToastService.RemoveToastAsync(Toast);
        }

        public void Dispose()
        {
            _countdownTimer?.Dispose();
            _countdownTimer = null;
        }
    }

}

