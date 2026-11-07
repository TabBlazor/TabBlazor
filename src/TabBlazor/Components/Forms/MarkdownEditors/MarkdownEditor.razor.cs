#nullable enable
using System.Linq.Expressions;
using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace TabBlazor;

/// <summary>
/// A markdown text area with a formatting toolbar, an optional rendered preview and optional file uploads.
/// Works inside and outside a form, posts as a plain text area in static server rendering, and sends the value
/// to .NET when the user pauses typing (see <see cref="DebounceMs"/>) or leaves the field, not on every key.
/// </summary>
/// <remarks>
/// The preview needs an <see cref="IMarkdownRenderer"/>, registered in DI (<c>services.AddTabBlazorMarkdown()</c>
/// from <c>TabBlazor.Markdown</c>) or passed to <see cref="Renderer"/>. Without one the editor has no preview.
/// File uploads are enabled by setting <see cref="UploadFile"/>.
/// <para>
/// On Blazor Server the value travels in one SignalR message, like any text input, and SignalR's default limit is
/// 32 KB. For longer documents raise it:
/// <c>.AddInteractiveServerComponents().AddHubOptions(o =&gt; o.MaximumReceiveMessageSize = 1024 * 1024)</c>.
/// </para>
/// </remarks>
public partial class MarkdownEditor : TablerBaseComponent, IAsyncDisposable
{
    private const string ModulePath = "./_content/TabBlazor/Components/Forms/MarkdownEditors/MarkdownEditor.razor.js";
    private const int ProgressRefreshMs = 100;

    private readonly string generatedId = $"markdown-editor-{Guid.CreateVersion7():N}";
    private readonly List<MarkdownUpload> uploads = [];
    private readonly List<MarkdownUploadError> uploadErrors = [];
    private ElementReference root;
    private IJSObjectReference? module;
    private Task<IJSObjectReference>? moduleTask;
    private FieldIdentifier? fieldIdentifier;
    private EditContext? subscribedEditContext;
    private string fieldCssClasses = "";
    private string? currentValue;
    private string? lastValueParameter;
    private bool valueSetByParent;
    private MarkdownEditorMode currentMode;
    private MarkdownEditorMode lastModeParameter;
    private IReadOnlyList<MarkdownEditorMode> availableModes = [MarkdownEditorMode.Edit];
    private string? lastClientOptions;
    private string? previewSource;
    private MarkupString previewMarkup;
    private MarkdownEditorText? appTexts;
    private long lastProgressRefresh;
    private bool wasUploading;
    private bool disposed;

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IServiceProvider Services { get; set; } = default!;

    /// <summary>The markdown. Supports two-way binding via <c>@bind-Value</c>.</summary>
    [Parameter] public string? Value { get; set; }
    /// <summary>Raised when the user pauses typing, leaves the field or changes the text with the toolbar.</summary>
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }
    /// <summary>Identifies the bound field for validation; set automatically by <c>@bind-Value</c>.</summary>
    [Parameter] public Expression<Func<string?>>? ValueExpression { get; set; }

    /// <summary>The label text shown above the editor.</summary>
    [Parameter] public string? Label { get; set; }
    /// <summary>Placeholder text shown while the text area is empty.</summary>
    [Parameter] public string? Placeholder { get; set; }
    /// <summary>
    /// The form field name. Defaults to the bound member path, such as <c>Model.Notes</c> for
    /// <c>@bind-Value="Model.Notes"</c>. Set it when a static server-rendered form binds through an indexer or a
    /// <c>FormMappingScope</c>.
    /// </summary>
    [Parameter] public string? Name { get; set; }
    /// <summary>Disables the text area, the toolbar and the view buttons.</summary>
    [Parameter] public bool Disabled { get; set; }
    /// <summary>Makes the text area read-only and disables the toolbar. The view buttons still work.</summary>
    [Parameter] public bool ReadOnly { get; set; }
    /// <summary>The minimum height in rows. The text area grows with its content. Defaults to 6.</summary>
    [Parameter] public int Rows { get; set; } = 6;
    /// <summary>Optional CSS length the text area stops growing at and scrolls instead, for example <c>"30rem"</c>.</summary>
    [Parameter] public string? MaxHeight { get; set; }
    /// <summary>
    /// Milliseconds of no typing before the value is sent to .NET. Defaults to 300. Null or 0 sends it only when the
    /// user leaves the field or uses the toolbar.
    /// </summary>
    [Parameter] public int? DebounceMs { get; set; } = 300;
    /// <summary>When false, hides the toolbar. Keyboard shortcuts and list continuation still work.</summary>
    [Parameter] public bool ShowToolbar { get; set; } = true;
    /// <summary>Extra toolbar content, for example buttons that call <see cref="InsertAsync"/>.</summary>
    [Parameter] public RenderFragment? ToolbarExtra { get; set; }

    /// <summary>What the editor shows. Supports two-way binding via <c>@bind-Mode</c>. Defaults to <see cref="MarkdownEditorMode.Edit"/>.</summary>
    [Parameter] public MarkdownEditorMode Mode { get; set; } = MarkdownEditorMode.Edit;
    /// <summary>Raised when the user switches between write, hybrid, preview and side by side.</summary>
    [Parameter] public EventCallback<MarkdownEditorMode> ModeChanged { get; set; }
    /// <summary>
    /// The views users can switch between, in the order of their buttons, for example
    /// <c>Modes="[MarkdownEditorMode.Edit, MarkdownEditorMode.Hybrid]"</c>. Defaults to all of them.
    /// <see cref="MarkdownEditorMode.Preview"/> and <see cref="MarkdownEditorMode.Split"/> are offered only with a
    /// renderer. With one view left there are no view buttons and the editor stays in that view. When
    /// <see cref="Mode"/> is not offered, the editor shows the first view that is.
    /// </summary>
    [Parameter] public IReadOnlyList<MarkdownEditorMode>? Modes { get; set; }
    /// <summary>Renders the preview. Defaults to the <see cref="IMarkdownRenderer"/> registered in DI.</summary>
    [Parameter] public IMarkdownRenderer? Renderer { get; set; }

    /// <summary>Overrides built-in UI strings.</summary>
    [Parameter] public MarkdownEditorText? Text { get; set; }

    /// <summary>
    /// Uploads a pasted, dropped or picked file and returns where it is. Setting it enables uploads. Throw a
    /// <see cref="MarkdownUploadException"/> to reject a file with a message for the user. The token is cancelled
    /// when the user cancels the upload or the editor goes away.
    /// </summary>
    [Parameter] public Func<MarkdownUploadFile, CancellationToken, Task<MarkdownUploadResult>>? UploadFile { get; set; }
    /// <summary>
    /// The file types users may upload, in the syntax of the HTML <c>accept</c> attribute, for example
    /// <c>"image/*,.pdf"</c>. Defaults to <c>"image/*"</c>. Null or <c>"*"</c> allows every type. This is a convenience
    /// filter, not a security check: validate files on the server.
    /// </summary>
    [Parameter] public string? AcceptedFileTypes { get; set; } = "image/*";
    /// <summary>The largest file users may upload, in bytes. Defaults to 10 MB.</summary>
    [Parameter] public long MaxFileSize { get; set; } = 10 * 1024 * 1024;
    /// <summary>Raised when a file is rejected or its upload fails. The user already sees the message.</summary>
    [Parameter] public EventCallback<MarkdownUploadError> OnUploadError { get; set; }
    /// <summary>
    /// Raised when an upload finishes after the user deleted its placeholder, so its link was not inserted.
    /// Use it to delete the stored file.
    /// </summary>
    [Parameter] public EventCallback<MarkdownUploadResult> OnUploadDiscarded { get; set; }
    /// <summary>Raised with true when the first upload starts and with false when the last one ends. See <see cref="IsUploading"/>.</summary>
    [Parameter] public EventCallback<bool> OnUploadStateChanged { get; set; }

    [CascadingParameter] private EditContext? CascadedEditContext { get; set; }

    /// <summary>True while files are uploading. Disable saving meanwhile, or the saved text contains placeholders.</summary>
    public bool IsUploading => uploads.Count > 0;

    internal MarkdownEditorText Texts => Text ?? (appTexts ??= AppTexts());

    protected string InputId => UnmatchedParameters?.TryGetValue("id", out var id) == true && id is not null ? id.ToString()! : generatedId;
    private string? NameValue => Name ?? MarkdownSyntax.FieldPath(ValueExpression);
    private IMarkdownRenderer? EffectiveRenderer => Renderer ?? Services.GetService<IMarkdownRenderer>();
    private bool HasRenderer => EffectiveRenderer is not null;
    private bool Interactive => RendererInfo.IsInteractive;
    private bool ToolbarVisible => Interactive && ShowToolbar;
    private bool UploadsEnabled => Interactive && UploadFile is not null && !Disabled && !ReadOnly;
    private bool ImagesOnly => MarkdownSyntax.AcceptsOnlyImages(AcceptedFileTypes);
    private MarkdownEditorMode EffectiveMode => availableModes.Contains(currentMode) ? currentMode : availableModes[0];
    // Without a renderer the default is no view buttons (Mode still works); listing Modes asks for them.
    private bool ShowModeButtons => availableModes.Count > 1 && (Modes is not null || HasRenderer);
    private bool Hybrid => EffectiveMode == MarkdownEditorMode.Hybrid;
    private bool ShowPreview => EffectiveMode is MarkdownEditorMode.Preview or MarkdownEditorMode.Split;
    private bool FormattingDisabled => Disabled || ReadOnly || EffectiveMode == MarkdownEditorMode.Preview;
    private string? AriaInvalid => fieldCssClasses.Contains("invalid", StringComparison.Ordinal) ? "true" : null;

    protected override string ClassNames => ClassBuilder
        .Add("markdown-editor")
        .AddIf("markdown-editor-disabled", Disabled)
        .AddIf(fieldCssClasses, !string.IsNullOrEmpty(fieldCssClasses))
        .ToString();

    private string BodyClassNames => new ClassBuilder("markdown-editor-body")
        .AddCompare("markdown-editor-body-preview", EffectiveMode, MarkdownEditorMode.Preview)
        .AddCompare("markdown-editor-body-split", EffectiveMode, MarkdownEditorMode.Split)
        .AddIf("markdown-editor-body-hybrid", Hybrid)
        .ToString();

    private string InputClassNames => new ClassBuilder("form-control markdown-editor-input")
        .AddIf(fieldCssClasses, !string.IsNullOrEmpty(fieldCssClasses))
        .ToString();

    private string ClientOptions => string.Join('|', DebounceMs ?? 0, MaxHeight, Disabled, ReadOnly, Texts.LinkTextPlaceholder, Texts.ImageTextPlaceholder, Texts.TableColumn, Hybrid);

    protected override void OnParametersSet()
    {
        if (Value != lastValueParameter)
        {
            lastValueParameter = Value;
            if (Value != currentValue)
            {
                currentValue = Value;
                valueSetByParent = true;
            }
        }

        if (Mode != lastModeParameter)
        {
            lastModeParameter = Mode;
            currentMode = Mode;
        }

        availableModes = AvailableModes();

        if (ValueExpression is not null)
        {
            fieldIdentifier = FieldIdentifier.Create(ValueExpression);
        }

        if (!ReferenceEquals(subscribedEditContext, CascadedEditContext))
        {
            if (subscribedEditContext is not null)
            {
                subscribedEditContext.OnValidationStateChanged -= OnValidationStateChanged;
            }
            subscribedEditContext = CascadedEditContext;
            if (subscribedEditContext is not null)
            {
                subscribedEditContext.OnValidationStateChanged += OnValidationStateChanged;
            }
        }

        UpdateFieldCssClasses();
        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!Interactive || disposed)
        {
            return;
        }

        var options = ClientOptions;
        if (module is null)
        {
            module = await GetModuleAsync();
            if (disposed)
            {
                return;
            }
            await module.InvokeVoidAsync("attach", root, CreateClientOptions());
            lastClientOptions = options;
            valueSetByParent = false;
            return;
        }

        if (options != lastClientOptions)
        {
            lastClientOptions = options;
            await module.InvokeVoidAsync("update", root, CreateClientOptions());
        }

        if (valueSetByParent)
        {
            valueSetByParent = false;
            await module.InvokeVoidAsync("refresh", root);
        }

        if (focusTableEditor && TableDialogOpen)
        {
            focusTableEditor = false;
            await module.InvokeVoidAsync("focusTableEditor", root);
        }
    }

    /// <summary>
    /// The offered views that work here: all of them by default, the preview views only with a renderer (Hybrid styles
    /// the text in the browser and needs none) and only the plain text area in static rendering. Never empty.
    /// </summary>
    private List<MarkdownEditorMode> AvailableModes()
    {
        if (!Interactive)
        {
            return [MarkdownEditorMode.Edit];
        }

        var hasRenderer = HasRenderer;
        var modes = (Modes ?? [MarkdownEditorMode.Edit, MarkdownEditorMode.Hybrid, MarkdownEditorMode.Preview, MarkdownEditorMode.Split])
            .Where(mode => Enum.IsDefined(mode) && (hasRenderer || mode is MarkdownEditorMode.Edit or MarkdownEditorMode.Hybrid))
            .Distinct()
            .ToList();
        return modes.Count > 0 ? modes : [MarkdownEditorMode.Edit];
    }

    private string ModeLabel(MarkdownEditorMode mode) => mode switch
    {
        MarkdownEditorMode.Hybrid => Texts.Hybrid,
        MarkdownEditorMode.Preview => Texts.Preview,
        MarkdownEditorMode.Split => Texts.Split,
        _ => Texts.Write
    };

    private Task<IJSObjectReference> GetModuleAsync() =>
        moduleTask ??= JS.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();

    private object CreateClientOptions() => new
    {
        debounceMs = DebounceMs is > 0 ? DebounceMs.Value : 0,
        maxHeight = MaxHeight,
        locked = Disabled || ReadOnly,
        linkText = Texts.LinkTextPlaceholder,
        imageText = Texts.ImageTextPlaceholder,
        columnText = Texts.TableColumn,
        hybrid = Hybrid
    };

    private async Task OnChangeAsync(ChangeEventArgs args)
    {
        var value = args.Value?.ToString() ?? "";
        if (value == (currentValue ?? ""))
        {
            return;
        }

        currentValue = value;
        lastValueParameter = value;
        await ValueChanged.InvokeAsync(value);

        if (fieldIdentifier is { } fid)
        {
            CascadedEditContext?.NotifyFieldChanged(fid);
        }
    }

    private async Task SetModeAsync(MarkdownEditorMode mode)
    {
        if (mode == currentMode)
        {
            return;
        }

        currentMode = mode;
        await ModeChanged.InvokeAsync(mode);
    }

    private MarkupString RenderPreview()
    {
        if (previewSource != currentValue || previewMarkup.Value is null)
        {
            previewSource = currentValue;
            previewMarkup = EffectiveRenderer?.Render(currentValue) ?? default;
        }
        return previewMarkup;
    }

    /// <summary>
    /// Inserts markdown at the caret, replacing the selection, as if the user typed it (it can be undone). Use it
    /// from <see cref="ToolbarExtra"/> buttons. Does nothing before the editor is interactive.
    /// </summary>
    public async Task InsertAsync(string markdown)
    {
        if (module is not null && !disposed && !string.IsNullOrEmpty(markdown))
        {
            await module.InvokeVoidAsync("insert", root, markdown);
        }
    }

    /// <summary>Moves the focus to the text area.</summary>
    public async Task FocusAsync()
    {
        if (module is not null && !disposed)
        {
            await module.InvokeVoidAsync("focus", root);
        }
    }

    private async Task OnFilesSelectedAsync(InputFileChangeEventArgs args)
    {
        if (UploadFile is null || module is null || disposed)
        {
            return;
        }

        var started = new List<(MarkdownUpload Upload, IBrowserFile File)>();
        foreach (var file in args.GetMultipleFiles(Math.Max(1, args.FileCount)))
        {
            if (!MarkdownSyntax.Accepts(AcceptedFileTypes, file.Name, file.ContentType))
            {
                await ReportErrorAsync(new MarkdownUploadError(file.Name, string.Format(Texts.FileTypeNotAccepted, file.Name), null));
            }
            else if (file.Size > MaxFileSize)
            {
                await ReportErrorAsync(new MarkdownUploadError(file.Name,
                    string.Format(Texts.FileTooLarge, file.Name, MarkdownSyntax.FormatSize(MaxFileSize)), null));
            }
            else
            {
                started.Add((new MarkdownUpload(Guid.NewGuid().ToString("N")[..12], file.Name, file.Size), file));
            }
        }

        if (started.Count == 0)
        {
            return;
        }

        var placeholders = string.Join('\n', started.Select(s => MarkdownSyntax.Placeholder(string.Format(Texts.Uploading, s.Upload.FileName), s.Upload.Token)));
        await module.InvokeVoidAsync("insert", root, placeholders);

        uploads.AddRange(started.Select(s => s.Upload));
        await NotifyUploadStateAsync();

        foreach (var (upload, file) in started)
        {
            _ = RunUploadAsync(upload, file);
        }
    }

    private async Task RunUploadAsync(MarkdownUpload upload, IBrowserFile browserFile)
    {
        try
        {
            var file = MarkdownUploadFile.FromBrowserFile(browserFile, MaxFileSize);
            file.Progress = bytes =>
            {
                upload.BytesRead = bytes;
                RefreshProgress();
            };

            var result = await UploadFile!(file, upload.Cancellation.Token);
            if (string.IsNullOrWhiteSpace(result?.Url))
            {
                throw new InvalidOperationException("UploadFile returned no URL.");
            }

            var replaced = await ReplacePlaceholderAsync(upload, MarkdownSyntax.ForUpload(result, file.Name, file.ContentType));
            if (!replaced && !disposed)
            {
                await OnUploadDiscarded.InvokeAsync(result);
            }
        }
        catch (OperationCanceledException) when (upload.Cancellation.IsCancellationRequested)
        {
            await ReplacePlaceholderAsync(upload, "");
        }
        catch (MarkdownUploadException exception)
        {
            await FailAsync(upload, exception.Message, exception);
        }
        catch (Exception exception)
        {
            await FailAsync(upload, string.Format(Texts.UploadFailed, upload.FileName), exception);
        }
        finally
        {
            uploads.Remove(upload);
            upload.Dispose();
            if (!disposed)
            {
                await NotifyUploadStateAsync();
                StateHasChanged();
            }
        }
    }

    private async Task FailAsync(MarkdownUpload upload, string message, Exception exception)
    {
        await ReplacePlaceholderAsync(upload, "");
        if (!disposed)
        {
            await ReportErrorAsync(new MarkdownUploadError(upload.FileName, message, exception));
        }
    }

    private async Task<bool> ReplacePlaceholderAsync(MarkdownUpload upload, string markdown)
    {
        if (module is null || disposed)
        {
            return false;
        }

        try
        {
            return await module.InvokeAsync<bool>("replace", root, upload.Token, markdown);
        }
        catch (JSDisconnectedException)
        {
            return false;
        }
        catch (ObjectDisposedException)
        {
            return false;
        }
    }

    private async Task ReportErrorAsync(MarkdownUploadError error)
    {
        uploadErrors.Add(error);
        await OnUploadError.InvokeAsync(error);
    }

    private void DismissError(MarkdownUploadError error) => uploadErrors.Remove(error);

    private void CancelUpload(MarkdownUpload upload) => upload.Cancellation.Cancel();

    private void RefreshProgress()
    {
        var now = Environment.TickCount64;
        if (now - Interlocked.Read(ref lastProgressRefresh) < ProgressRefreshMs)
        {
            return;
        }

        Interlocked.Exchange(ref lastProgressRefresh, now);
        _ = InvokeAsync(() =>
        {
            if (!disposed)
            {
                StateHasChanged();
            }
        });
    }

    private async Task NotifyUploadStateAsync()
    {
        if (IsUploading != wasUploading)
        {
            wasUploading = IsUploading;
            await OnUploadStateChanged.InvokeAsync(wasUploading);
        }
    }

    private MarkdownEditorText AppTexts()
    {
        var options = Services.GetService<IOptions<TablerOptions>>()?.Value;
        var fallback = options?.MarkdownEditorText ?? MarkdownEditorText.Default;
        if (options?.MarkdownEditorTextResource is { } resource && Services.GetService<IStringLocalizerFactory>() is { } factory)
        {
            return MarkdownEditorText.FromResource(factory, resource, fallback);
        }
        return fallback;
    }

    private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs args)
    {
        UpdateFieldCssClasses();
        _ = InvokeAsync(StateHasChanged);
    }

    private void UpdateFieldCssClasses()
    {
        fieldCssClasses = fieldIdentifier is { } fid && CascadedEditContext is { } editContext
            ? editContext.FieldCssClass(fid)
            : "";
    }

    public async ValueTask DisposeAsync()
    {
        disposed = true;

        if (subscribedEditContext is not null)
        {
            subscribedEditContext.OnValidationStateChanged -= OnValidationStateChanged;
        }

        foreach (var upload in uploads.ToList())
        {
            upload.Cancellation.Cancel();
        }

        if (module is not null)
        {
            try
            {
                await module.InvokeVoidAsync("detach", root);
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        GC.SuppressFinalize(this);
    }
}
