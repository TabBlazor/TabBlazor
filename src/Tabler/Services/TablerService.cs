using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tabler.Services
{
    public class TablerService
    {
        private readonly IJSRuntime jSRuntime;

        public TablerService(IJSRuntime jSRuntime)
        {
            this.jSRuntime = jSRuntime;
        }

        public async Task ScrollToFragment(string fragmentId)
        {
             await jSRuntime.InvokeVoidAsync("blazorTabler.scrollToFragment", fragmentId);
        }

        public async Task ShowAlert(string message)
        {
            await jSRuntime.InvokeVoidAsync("blazorTabler.showAlert", message);
        }

    }
}
