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
                _countdownTimer = new CountdownTimer(Toast.Options.Delay);
                _countdownTimer.OnTick += CalculateProgress;
                _countdownTimer.OnElapsed += async () =>  { await Close(); };
                _countdownTimer.Start();
            } 
        }

        private async void CalculateProgress(int percentComplete)
        {
            _progress = 100 - percentComplete;
            await InvokeAsync(StateHasChanged);
        }

        private async Task Close()
        {
            //ToastsContainer.RemoveToast(ToastId);
            await ToastService.RemoveToastAsync(Toast);
        }

        private void ToastClick()
        {
           // ToastSettings.OnClick?.Invoke();
        }

        public void Dispose()
        {
            _countdownTimer?.Dispose();
            _countdownTimer = null;
        }
    }

}

