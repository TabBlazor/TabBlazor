using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace TabBlazor.Services
{
    public class ToastService
    {
        private List<Toast> toasts = new List<Toast>();

        public IEnumerable<Toast> Toasts => toasts;

        public async Task AddToastAsync(Toast toast)
        {
            toasts.Add(toast);
            await Changed();
            if (toast.Options.Delay > 0)
            {

#pragma warning disable 4014
                Task.Run(async () =>
#pragma warning restore 4014
                {
                    await Task.Delay(toast.Options.Delay);
                    await RemoveToastAsync(toast);
                   
                });
            }
        }

        public async Task RemoveToastAsync(Toast toast)
        {
            if (toasts.Contains(toast))
            {
                toasts.Remove(toast);
            }
            await Changed();
        }

        public async Task Changed()
        {
            if (OnChanged != null)
            {
                await OnChanged.Invoke();
            }
        }

        public event Func<Task> OnChanged;
    }
}