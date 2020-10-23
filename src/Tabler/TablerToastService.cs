using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tabler
{
    public class TablerToastService
    {
        public class Toast
        {
            public string Title { get; set; }
            public int Delay { get; set; } = 3000;
        }

        public List<Toast> Toasts { get; set; } = new List<Toast>();

        public async Task AddToast(Toast toast)
        {
            Toasts.Add(toast);
            await Changed();
#pragma warning disable 4014
            Task.Run(async () =>
#pragma warning restore 4014
            {
                await Task.Delay(toast.Delay);
                Toasts.Remove(toast);
                await Changed();
            });
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