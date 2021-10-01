using Microsoft.AspNetCore.Components;
using Tabler.Docs.Services;

namespace Tabler.Docs.Shared
{
    public partial class MainNavigation : ComponentBase
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
