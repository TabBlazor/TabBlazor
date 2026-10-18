using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using TabBlazor.Services;

namespace TabBlazor;

/// <summary>
/// Renders a single open offcanvas panel and its backdrop. Created by <see cref="OffcanvasContainer"/> for
/// <see cref="IOffcanvasService"/> and by the declarative <see cref="Offcanvas"/> component.
/// </summary>
public partial class OffcanvasView : ComponentBase, IDisposable, IAsyncDisposable
{
    [Inject] private OverlayLayerService OverlayLayers { get; set; }
    [Inject] private TablerService TablerService { get; set; }

    /// <summary>The offcanvas title.</summary>
    [Parameter] public string Title { get; set; }
    /// <summary>Custom title markup; replaces <see cref="Title"/>.</summary>
    [Parameter] public RenderFragment TitleTemplate { get; set; }
    /// <summary>The offcanvas appearance/behavior options (position, size, backdrop, close triggers, etc.).</summary>
    [Parameter] public OffcanvasOptions Options { get; set; }
    /// <summary>The offcanvas body content.</summary>
    [Parameter] public RenderFragment ChildContent { get; set; }
    /// <summary>Content of a footer below the body, for example Save and Cancel buttons.</summary>
    [Parameter] public RenderFragment FooterTemplate { get; set; }
    /// <summary>
    /// Raised before the panel closes by its close button, Esc or a click outside. Set
    /// <see cref="OffcanvasClosingEventArgs.Cancel"/> to keep it open. <see cref="OffcanvasOptions.OnClosing"/> runs after it.
    /// </summary>
    [Parameter] public EventCallback<OffcanvasClosingEventArgs> OnClosing { get; set; }
    /// <summary>Raised when the offcanvas is closed by its close button, Esc or a click outside.</summary>
    [Parameter] public EventCallback OnClosed { get; set; }
    /// <summary>Plays the closing animation.</summary>
    [Parameter] public bool Hiding { get; set; }

    private readonly string id = $"offcanvas-{Guid.CreateVersion7():N}";
    private readonly string titleId = $"offcanvas-title-{Guid.CreateVersion7():N}";
    private ElementReference panel;
    private int zIndex;
    private bool clickOutsideActive;
    private bool focusSaved;
    private bool disposed;

    private bool IsTopMost => OverlayLayers.IsTopMost(zIndex);
    private bool ShouldCloseOnClickOutside => Options.CloseOnClickOutside && IsTopMost && !Hiding;

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
            // Remember what had focus, so it gets focus back when the panel closes.
            await TablerService.SaveFocusAsync(id);
            focusSaved = true;
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

    private async Task Close()
    {
        var args = new OffcanvasClosingEventArgs();
        if (OnClosing.HasDelegate)
        {
            await OnClosing.InvokeAsync(args);
        }

        if (!args.Cancel && Options.OnClosing != null)
        {
            await Options.OnClosing(args);
        }

        if (!args.Cancel)
        {
            await OnClosed.InvokeAsync();
        }
    }

    private Task OnClickOutside() => ShouldCloseOnClickOutside ? Close() : Task.CompletedTask;

    private Task OnKeyDown(KeyboardEventArgs e) =>
        e.Key == "Escape" && Options.CloseOnEsc && IsTopMost ? Close() : Task.CompletedTask;

    private string ClassNames => new ClassBuilder()
        .Add("offcanvas")
        .Add($"offcanvas-{Options.Position.ToString().ToLower()}")
        .AddIf("offcanvas-narrow", Options.Narrow)
        .AddIf("offcanvas-floating", Options.Floating)
        .AddIf($"offcanvas-size-{Options.Size.ToString().ToLower()}", Options.Size != OffcanvasSize.Default)
        .Add(Options.WrapperCssClass)
        .Add("show")
        .AddIf("offcanvas-hiding", Hiding)
        .ToString();

    private string Style
    {
        get
        {
            var size = SizeValue(Options.Size, Options.Position is OffcanvasPosition.Top or OffcanvasPosition.Bottom);
            if (size == null)
            {
                return $"z-index: {zIndex}";
            }

            var dimension = Options.Position is OffcanvasPosition.Top or OffcanvasPosition.Bottom ? "height" : "width";
            return $"z-index: {zIndex}; --tblr-offcanvas-{dimension}: {size}; --bs-offcanvas-{dimension}: {size}";
        }
    }

    private static string SizeValue(OffcanvasSize size, bool vertical) => size switch
    {
        OffcanvasSize.Small => vertical ? "25vh" : "min(20rem, 100vw)",
        OffcanvasSize.Medium => vertical ? "40vh" : "min(30rem, 100vw)",
        OffcanvasSize.Large => vertical ? "70vh" : "min(50rem, 100vw)",
        OffcanvasSize.Full => vertical ? "100vh" : "100vw",
        _ => null
    };

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        OverlayLayers.OnChanged -= OnOverlayLayersChanged;
        OverlayLayers.Unregister(this);
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();
        if (!focusSaved)
        {
            return;
        }

        try
        {
            await TablerService.RestoreFocusAsync(id);
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone; there is nothing to give focus back to.
        }
        catch (TaskCanceledException)
        {
        }
    }
}
