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

