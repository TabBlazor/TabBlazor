namespace TabBlazor.Services
{
    public class ToastService
    {
        private List<ToastModel> toasts = new List<ToastModel>();
        private ReaderWriterLockSlim listLock = new ReaderWriterLockSlim();
        public IEnumerable<ToastModel> Toasts => toasts;

        public async Task AddToastAsync(ToastModel toast)
        {
            AddToast(toast);
            await UpdateAsync();
        }

        public async Task AddToastAsync<TComponent>(string title, string subTitle, RenderComponent<TComponent> component, ToastOptions options = null) where TComponent : IComponent
        {
            var toast = new ToastModel(title, subTitle, component?.Contents, options);
            await AddToastAsync(toast);
        }

        private void AddToast(ToastModel toast)
        {
            try
            {
                listLock.EnterWriteLock();
                toasts.Add(toast);

            }
            finally
            {
                listLock.ExitWriteLock();
            }
        }

        public async Task RemoveAllAsync()
        {
            toasts.Clear();
            await UpdateAsync();
        }

        public async Task RemoveToastAsync(ToastModel toast)
        {
            try
            {
                listLock.EnterWriteLock();
                if (toasts.Contains(toast))
                {
                    toasts.Remove(toast);
                }
            }
            finally
            {
                listLock.ExitWriteLock();
            }

            await UpdateAsync();
        }

        [Obsolete("Please use UpdateAsync, will be removed in future release")]
        public async Task Changed()
        {
            await UpdateAsync();
        }

        public async Task UpdateAsync()
        {
            if (OnChanged != null)
            {
                await OnChanged.Invoke();
            }
           
        }

        public event Func<Task> OnChanged;
    }
}