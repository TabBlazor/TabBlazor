using Microsoft.AspNetCore.Components;
using System;
using Tabler.Docs.Services;

namespace Tabler.Docs.Shared
{
    public partial class MainLayout : LayoutComponentBase, IDisposable
    {
        [Inject] private AppService appService { get; set; }

       
        protected override void OnInitialized()
        {
            appService.OnSettingsUpdated += SettingsUpdated;
        }

        private void SettingsUpdated()
        {
            InvokeAsync(() => StateHasChanged());
        }
        public void Dispose()
        {
            appService.OnSettingsUpdated -= SettingsUpdated;
        }
    }
}
