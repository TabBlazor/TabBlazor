using TabBlazor.Services;

namespace TabBlazor
{
    public partial class CheckboxTriState : ComponentBase
    {
        private bool isInitialized;
        [Inject] public TablerService TablerService { get; set; }
        [Parameter] public string Label { get; set; }
        [Parameter] public string Description { get; set; }

        [Parameter] public bool? Value { get; set; }

        [Parameter] public EventCallback<bool?> ValueChanged { get; set; }
        [Parameter] public EventCallback Changed { get; set; }
        [Parameter] public bool Disabled { get; set; }

        protected ElementReference Element { get; set; }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender || isInitialized)
            {
                isInitialized = true;
                await TablerService.SetElementProperty(Element, "indeterminate", !Value.HasValue);
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