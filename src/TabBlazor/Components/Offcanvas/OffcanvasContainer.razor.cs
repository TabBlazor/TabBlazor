namespace TabBlazor;

public partial class OffcanvasContainer : IDisposable
{
    [Inject] private IOffcanvasService offcanvasService { get; set; }

    protected override void OnInitialized()
    {
        offcanvasService.OnChanged += StateHasChanged;

        base.OnInitialized();
    }

    public void Dispose()
    {
        offcanvasService.OnChanged -= StateHasChanged;
    }
}
