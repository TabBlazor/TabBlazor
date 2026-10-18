using Microsoft.Extensions.DependencyInjection;

namespace TabBlazor;

/// <summary>
/// A declarative offcanvas panel: <c>&lt;Offcanvas @bind-Open="open" Title="Order"&gt;…&lt;/Offcanvas&gt;</c>.
/// Its content re-renders with the page, so it can show the current item without closing and reopening.
/// It shares markup and styles with panels opened through <see cref="IOffcanvasService"/>.
/// </summary>
public partial class Offcanvas : ComponentBase, IDisposable
{
    /// <summary>How long the closing animation plays before the panel is removed.</summary>
    internal static readonly TimeSpan HideDuration = TimeSpan.FromMilliseconds(250);

    private OffcanvasOptions options = new();
    private bool visible;
    private bool hiding;
    private CancellationTokenSource hideCts;

    [Inject] private IServiceProvider Services { get; set; }

    /// <summary>Whether the panel is open. Use <c>@bind-Open</c>.</summary>
    [Parameter] public bool Open { get; set; }
    /// <summary>Raised when the panel closes itself (close button, Esc, click outside).</summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }
    /// <summary>The title in the header.</summary>
    [Parameter] public string Title { get; set; }
    /// <summary>Custom title markup; replaces <see cref="Title"/>.</summary>
    [Parameter] public RenderFragment TitleTemplate { get; set; }
    /// <summary>The panel content.</summary>
    [Parameter] public RenderFragment ChildContent { get; set; }
    /// <summary>A footer below the content, for example Save and Cancel buttons.</summary>
    [Parameter] public RenderFragment FooterTemplate { get; set; }
    /// <summary>The edge the panel slides in from. Defaults to <see cref="OffcanvasPosition.End"/>.</summary>
    [Parameter] public OffcanvasPosition Position { get; set; } = OffcanvasPosition.End;
    /// <summary>The panel's width (start/end) or height (top/bottom).</summary>
    [Parameter] public OffcanvasSize Size { get; set; } = OffcanvasSize.Default;
    /// <summary>Shows a backdrop behind the panel. Defaults to true.</summary>
    [Parameter] public bool Backdrop { get; set; } = true;
    /// <summary>Closes the panel on Esc. Defaults to true.</summary>
    [Parameter] public bool CloseOnEsc { get; set; } = true;
    /// <summary>Closes the panel when clicking outside it. Defaults to false.</summary>
    [Parameter] public bool CloseOnClickOutside { get; set; }
    /// <summary>Detaches the panel from the screen edge.</summary>
    [Parameter] public bool Floating { get; set; }
    /// <summary>Extra CSS classes for the panel.</summary>
    [Parameter] public string Class { get; set; }
    /// <summary>
    /// Raised before the panel closes itself. Set <see cref="OffcanvasClosingEventArgs.Cancel"/> to keep it open,
    /// for example after asking "Discard changes?".
    /// </summary>
    [Parameter] public EventCallback<OffcanvasClosingEventArgs> OnClosing { get; set; }
    /// <summary>Raised after the panel has closed.</summary>
    [Parameter] public EventCallback OnClosed { get; set; }

    protected override void OnParametersSet()
    {
        options = new OffcanvasOptions
        {
            Position = Position,
            Size = Size,
            Backdrop = Backdrop,
            CloseOnEsc = CloseOnEsc,
            CloseOnClickOutside = CloseOnClickOutside,
            Floating = Floating,
            WrapperCssClass = Class
        };

        if (Open)
        {
            hideCts?.Cancel();
            visible = true;
            hiding = false;
        }
        else if (visible && !hiding)
        {
            _ = HideAsync();
        }
    }

    /// <summary>Asks to close the panel: <see cref="OnClosing"/> can keep it open.</summary>
    public async Task CloseAsync()
    {
        var args = new OffcanvasClosingEventArgs();
        if (OnClosing.HasDelegate)
        {
            await OnClosing.InvokeAsync(args);
        }

        if (!args.Cancel)
        {
            await OnViewClosedAsync();
        }
    }

    // The view has already run OnClosing.
    private async Task OnViewClosedAsync()
    {
        Open = false;
        await OpenChanged.InvokeAsync(false);
        if (visible && !hiding)
        {
            await HideAsync();
        }
    }

    private async Task HideAsync()
    {
        hiding = true;
        StateHasChanged();
        var cts = hideCts = new CancellationTokenSource();
        var timeProvider = Services.GetService<TimeProvider>() ?? TimeProvider.System;
        try
        {
            await Task.Delay(HideDuration, timeProvider, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await InvokeAsync(async () =>
        {
            if (cts.IsCancellationRequested)
            {
                return;
            }

            visible = false;
            hiding = false;
            StateHasChanged();
            await OnClosed.InvokeAsync();
        });
    }

    public void Dispose() => hideCts?.Cancel();
}
