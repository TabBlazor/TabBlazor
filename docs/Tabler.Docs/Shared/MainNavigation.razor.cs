using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Tabler.Docs.Shared
{
    public partial class MainNavigation : ComponentBase
    {
        [Parameter] public bool DarkMode { get; set; }
        [Parameter] public EventCallback<bool> DarkModeChanged { get; set; }


        private async Task DarkModeUpdated(bool value)
        {
            DarkMode = value;
            await DarkModeChanged.InvokeAsync(value);
        }
    }
}
