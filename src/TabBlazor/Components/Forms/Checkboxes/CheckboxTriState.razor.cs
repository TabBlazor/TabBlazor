using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using TabBlazor.Services;

namespace TabBlazor
{
    public partial class CheckboxTriState : ComponentBase
    {
        [Inject] public TablerService TablerService { get; set; }
        [Parameter] public string Label { get; set; }
        [Parameter] public string Description { get; set; }
        [Parameter] public bool? Value { get; set; }
        [Parameter] public EventCallback<bool?> ValueChanged { get; set; }
        [Parameter] public EventCallback Changed { get; set; }
        [Parameter] public bool Disabled { get; set; }

        protected ElementReference element { get; set; }

        private bool isInitialized;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender || isInitialized)
            {
                isInitialized = true;
                await TablerService.SetElementProperty(element, "indeterminate", !Value.HasValue);
            }
        }

        protected async Task ToggleState()
        {
            if (Value == null)
            {
                Value = true;
            }
            else
            {
                Value = !Value;
            }

            await ValueChanged.InvokeAsync(Value);
            await Changed.InvokeAsync();
        }
    }
}