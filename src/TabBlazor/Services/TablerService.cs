using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace TabBlazor.Services
{
    public class TablerService
    {
        private readonly IJSRuntime jsRuntime;

        public TablerService(IJSRuntime jSRuntime)
        {
            this.jsRuntime = jSRuntime;
        }

        public async Task SaveAsFile(string fileName, string href)
        {
            await jsRuntime.InvokeVoidAsync("tabBlazor.saveAsFile", fileName, href);
        }

   

        public async Task PreventDefaultKey(ElementReference element, string eventName, string[] keys)
        {
            await jsRuntime.InvokeVoidAsync("tabBlazor.preventDefaultKey", element, eventName, keys);
        }

        public async Task FocusFirstInTableRow(ElementReference tableRow)
        {
            await jsRuntime.InvokeVoidAsync("tabBlazor.focusFirstInTableRow", tableRow, "");
        }

        public async Task NavigateTable(ElementReference tableCell, string key)
        {
            await jsRuntime.InvokeVoidAsync("tabBlazor.navigateTable", tableCell, key);
        }

        public async Task ScrollToFragment(string fragmentId)
        {
            await jsRuntime.InvokeVoidAsync("tabBlazor.scrollToFragment", fragmentId);
        }

        public async Task ShowAlert(string message)
        {
            await jsRuntime.InvokeVoidAsync("tabBlazor.showAlert", message);
        }

        public async Task CopyToClipboard(string text)
        {
            await jsRuntime.InvokeVoidAsync("tabBlazor.copyToClipboard", text);
        }

        public async Task<string> ReadFromClipboard()
        {
            return await jsRuntime.InvokeAsync<string>("tabBlazor.readFromClipboard");
        }

        public async Task DisableDraggable(ElementReference container, ElementReference element)
        {
            await jsRuntime.InvokeVoidAsync("tabBlazor.disableDraggable", container, element);
        }

        public async Task SetElementProperty(ElementReference element, string property, object value)
        {
            await jsRuntime.InvokeVoidAsync("tabBlazor.setPropByElement", element, property, value);
        }

    }


}
