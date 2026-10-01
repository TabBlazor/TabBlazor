using Microsoft.AspNetCore.Components.Web;
using TabBlazor.Services;

namespace TabBlazor;

/// <summary>
/// Renders a single open offcanvas panel and its backdrop. Created internally by
/// <see cref="OffcanvasContainer"/>; you usually don't place this directly.
/// </summary>
public partial class OffcanvasView : ComponentBase, IDisposable
{
    [Inject] private OverlayLayerService OverlayLayers { get; set; }

    /// <summary>The offcanvas title.</summary>
    [Parameter] public string Title { get; set; }
    /// <summary>The offcanvas appearance/behavior options (position, backdrop, close triggers, etc.).</summary>
    [Parameter] public OffcanvasOptions Options { get; set; }
    /// <summary>The offcanvas body content.</summary>
    [Parameter] public RenderFragment ChildContent { get; set; }
    /// <summary>Raised when the offcanvas is closed by its close button, Esc or a click outside.</summary>
    [Parameter] public EventCallback OnClosed { get; set; }

    private readonly string titleId = $"offcanvas-title-{Guid.CreateVersion7():N}";
    private ElementReference panel;
    private int zIndex;
    private bool clickOutsideActive;

    private bool IsTopMost => OverlayLayers.IsTopMost(zIndex);
    private bool ShouldCloseOnClickOutside => Options.CloseOnClickOutside && IsTopMost;

    protected override void OnInitialized()
    {
        zIndex = OverlayLayers.Register(this);
        OverlayLayers.OnChanged += OnOverlayLayersChanged;
    }

    protected override void OnParametersSet()
    {
        clickOutsideActive = ShouldCloseOnClickOutside;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await panel.FocusAsync();
        }
    }

    private void OnOverlayLayersChanged()
    {
        if (clickOutsideActive == ShouldCloseOnClickOutside)
        {
            return;
        }

        clickOutsideActive = ShouldCloseOnClickOutside;
        _ = InvokeAsync(StateHasChanged);
    }

    private Task Close() => OnClosed.InvokeAsync();

    private Task OnClickOutside() => ShouldCloseOnClickOutside ? Close() : Task.CompletedTask;

    private Task OnKeyDown(KeyboardEventArgs e) =>
        e.Key == "Escape" && Options.CloseOnEsc && IsTopMost ? Close() : Task.CompletedTask;

    private string ClassNames => new ClassBuilder()
        .Add("offcanvas")
        .Add($"offcanvas-{Options.Position.ToString().ToLower()}")
        .AddIf("offcanvas-narrow", Options.Narrow)
        .AddIf("offcanvas-floating", Options.Floating)
        .Add(Options.WrapperCssClass)
        .Add("show")
        .ToString();

    public void Dispose()
    {
        OverlayLayers.OnChanged -= OnOverlayLayersChanged;
        OverlayLayers.Unregister(this);
    }
}
