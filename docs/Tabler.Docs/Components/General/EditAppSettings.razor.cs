using Microsoft.AspNetCore.Components;
using Tabler.Docs.Services;

namespace Tabler.Docs.Components.General
{
    public partial class EditAppSettings : ComponentBase
    {
        [Inject] private AppService appService { get; set; }

        private void Update()
        {
            appService.SettingsUpdated();
        }
    }
}
