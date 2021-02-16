using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace TabBlazor.Services
{
    public class ToastService
    {
        private List<ToastModel> toasts = new List<ToastModel>();

        public IEnumerable<ToastModel> Toasts => toasts;

        public async Task AddToastAsync(ToastModel toast)
        {
            toasts.Add(toast);
            await Changed();
        }

        public async Task RemoveToastAsync(ToastModel toast)
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