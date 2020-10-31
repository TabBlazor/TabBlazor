using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Tabler.Components
{
    public partial class ToastServiceComp : ComponentBase
    {
        [Inject] public ToastService ToastService { get; set; }

        protected override void OnInitialized()
        {
            ToastService.OnChanged += OnToastChanged;
        }

        public async Task OnToastChanged()
        {
            await InvokeAsync(() => { StateHasChanged(); });
        }

        public void Dispose()
        {
           ToastService.OnChanged -= OnToastChanged;
        }
    }
}