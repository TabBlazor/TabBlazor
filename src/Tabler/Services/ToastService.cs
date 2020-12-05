using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace TabBlazor.Services
{
    public class ToastService
    {
        public class Toast
        {
            public string Title { get; set; }
            public string SubTitle { get; set; }
            public string Message { get; set; }
            public int Delay { get; set; } = 3000;
            public RenderFragment Body { get; set; }
            public RenderFragment Header { get; set; }
        }

        public List<Toast> Toasts { get; set; } = new List<Toast>();

        public async Task AddToastAsync(Toast toast)
        {
            Toasts.Add(toast);
            await Changed();
            if (toast.Delay > 0)
            {

#pragma warning disable 4014
                Task.Run(async () =>
#pragma warning restore 4014
                {
                    await Task.Delay(toast.Delay);
                    Toasts.Remove(toast);
                    await Changed();
                });
            }
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