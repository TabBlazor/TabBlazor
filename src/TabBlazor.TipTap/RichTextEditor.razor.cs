using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace TabBlazor;

/// <summary>
/// A rich text (WYSIWYG) editor built on TipTap, with a Tabler toolbar, optional uploads and <c>@</c> mentions. The value
/// is HTML by default, or the TipTap JSON document (<see cref="Format"/>). Works inside and outside a form, and sends
/// the value to .NET when the user pauses typing (see <see cref="DebounceMs"/>) or leaves the field, not on every key.
/// </summary>
/// <remarks>
/// HTML posted by a browser can contain anything: sanitize it on the server where it is saved, and show it with
/// <see cref="RichTextView"/>. On Blazor Server the value travels in one SignalR message, and SignalR's default limit is
/// 32 KB; raise it for longer documents:
/// <c>.AddInteractiveServerComponents().AddHubOptions(o =&gt; o.MaximumReceiveMessageSize = 1024 * 1024)</c>.
/// </remarks>
public partial class RichTextEditor : TablerBaseComponent, IAsyncDisposable
{
    private const string ModulePath = "./_content/TabBlazor.TipTap/RichTextEditor.razor.js";
    private const int ProgressRefreshMs = 100;

    private readonly string generatedId = $"rich-text-editor-{Guid.CreateVersion7():N}";
    private readonly List<EditorUpload> uploads = [];
    private readonly List<EditorUploadError> uploadErrors = [];
    private ElementReference root;
    private IJSObjectReference? module;
    private Task<IJSObjectReference>? moduleTask;
    private DotNetObjectReference<RichTextEditor>? selfReference;
    private CancellationTokenSource? mentionSearch;
    private FieldIdentifier? fieldIdentifier;
    private EditContext? subscribedEditContext;
    private string fieldCssClasses = "";
    private string? currentValue;
    private string? lastValueParameter;
    private bool valueSetByParent;
    private string? lastClientOptions;
    private long lastProgressRefresh;
    private bool wasUploading;
    private bool disposed;

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IServiceProvider Services { get; set; } = default!;

    /// <summary>The content, as HTML or JSON (see <see cref="Format"/>). Null when empty. Supports <c>@bind-Value</c>.</summary>
    [Parameter] public string? Value { get; set; }
    /// <summary>Raised when the user pauses typing, leaves the field or uses the toolbar.</summary>
    [Parameter] public EventCallback<string?> ValueChanged { get; set; }
    /// <summary>Identifies the bound field for validation; set automatically by <c>@bind-Value</c>.</summary>
    [Parameter] public Expression<Func<string?>>? ValueExpression { get; set; }

    /// <summary>What <see cref="Value"/> holds. Defaults to <see cref="RichTextEditorFormat.Html"/>. Read when the editor starts.</summary>
    [Parameter] public RichTextEditorFormat Format { get; set; } = RichTextEditorFormat.Html;
    /// <summary>The label text shown above the editor.</summary>
    [Parameter] public string? Label { get; set; }
    /// <summary>Text shown while the editor is empty.</summary>
    [Parameter] public string? Placeholder { get; set; }
    /// <summary>
    /// The form field name of the hidden input that carries the value. Defaults to the bound member path, such as
    /// <c>Model.Body</c> for <c>@bind-Value="Model.Body"</c>.
    /// </summary>
    [Parameter] public string? Name { get; set; }
    /// <summary>Disables the editor and the toolbar.</summary>
    [Parameter] public bool Disabled { get; set; }
    /// <summary>Makes the content read-only and hides the toolbar.</summary>
    [Parameter] public bool ReadOnly { get; set; }
    /// <summary>CSS length of the smallest editor height. Defaults to <c>8rem</c>.</summary>
    [Parameter] public string? MinHeight { get; set; } = "8rem";
    /// <summary>CSS length the editor stops growing at and scrolls instead, for example <c>"30rem"</c>.</summary>
    [Parameter] public string? MaxHeight { get; set; }
    /// <summary>
    /// Milliseconds of no typing before the value is sent to .NET. Defaults to 300. Null or 0 sends it only when the
    /// user leaves the editor or uses the toolbar.
    /// </summary>
    [Parameter] public int? DebounceMs { get; set; } = 300;
    /// <summary>The built-in features. Defaults to <see cref="RichTextExtensions.Default"/>. Read when the editor starts.</summary>
    [Parameter] public RichTextExtensions Extensions { get; set; } = RichTextExtensions.Default;
    /// <summary>
    /// App-defined extensions to turn on, by the names their modules registered (see
    /// <see cref="TipTapOptions.ExtensionModules"/>). Read when the editor starts.
    /// </summary>
    [Parameter] public IReadOnlyList<string>? CustomExtensions { get; set; }
    /// <summary>Options for <see cref="CustomExtensions"/>, by extension name. Serialized to JSON and passed to each factory.</summary>
    [Parameter] public IReadOnlyDictionary<string, object?>? ExtensionOptions { get; set; }
    /// <summary>When false, hides the toolbar. Keyboard shortcuts still work.</summary>
    [Parameter] public bool ShowToolbar { get; set; } = true;
    /// <summary>
    /// The toolbar buttons, in order; <see cref="RichTextTool.Separator"/> starts a new group. Defaults to
    /// <see cref="RichTextToolbar.Full"/>; <see cref="RichTextToolbar.Minimal"/> has just the essentials. Buttons whose
    /// feature is off are left out. This only picks buttons: what content is allowed is <see cref="Extensions"/>, so
    /// keyboard shortcuts and pasted formatting still work without a button.
    /// </summary>
    [Parameter] public IReadOnlyList<RichTextTool>? Toolbar { get; set; }
    /// <summary>
    /// Extra toolbar content. A button with <c>data-rt-command="toggleBold"</c> (and optional JSON in
    /// <c>data-rt-args</c>) runs that TipTap command in the browser; or call <see cref="RunCommandAsync"/>.
    /// </summary>
    [Parameter] public RenderFragment? ToolbarExtra { get; set; }
    /// <summary>The largest number of characters. Shows a counter under the editor.</summary>
    [Parameter] public int? MaxLength { get; set; }
    /// <summary>Overrides the texts. Defaults to <see cref="TipTapOptions.Text"/>.</summary>
    [Parameter] public RichTextEditorText? Text { get; set; }

    /// <summary>
    /// Uploads a pasted, dropped or picked file and returns where it is. Setting it enables uploads. Throw an
    /// <see cref="EditorUploadException"/> to reject a file with a message for the user. The token is cancelled when
    /// the user cancels the upload or the editor goes away.
    /// </summary>
    [Parameter] public Func<EditorUploadFile, CancellationToken, Task<EditorUploadResult>>? UploadFile { get; set; }
    /// <summary>
    /// The file types users may upload, in the syntax of the HTML <c>accept</c> attribute. Defaults to <c>"image/*"</c>.
    /// Null or <c>"*"</c> allows every type. This is a convenience filter, not a security check.
    /// </summary>
    [Parameter] public string? AcceptedFileTypes { get; set; } = "image/*";
    /// <summary>The largest file users may upload, in bytes. Defaults to 10 MB.</summary>
    [Parameter] public long MaxFileSize { get; set; } = 10 * 1024 * 1024;
    /// <summary>Raised when a file is rejected or its upload fails. The user already sees the message.</summary>
    [Parameter] public EventCallback<EditorUploadError> OnUploadError { get; set; }
    /// <summary>Raised when an upload finishes but could not be inserted, for example because the editor closed. Delete the file.</summary>
    [Parameter] public EventCallback<EditorUploadResult> OnUploadDiscarded { get; set; }
    /// <summary>Raised with true when the first upload starts and with false when the last one ends.</summary>
    [Parameter] public EventCallback<bool> OnUploadStateChanged { get; set; }

    /// <summary>Finds the people offered after <c>@</c>. Needed for <see cref="RichTextExtensions.Mention"/>.</summary>
    [Parameter] public Func<string, CancellationToken, Task<IReadOnlyList<RichTextMention>>>? MentionSearch { get; set; }
    /// <summary>The character that starts a mention. Defaults to <c>@</c>.</summary>
    [Parameter] public char MentionTrigger { get; set; } = '@';
    /// <summary>Characters typed after the trigger before the first search. Defaults to 0.</summary>
    [Parameter] public int MentionMinChars { get; set; }
    /// <summary>Milliseconds of no typing before a search. Defaults to 150.</summary>
    [Parameter] public int MentionDebounceMs { get; set; } = 150;

    [CascadingParameter] private EditContext? CascadedEditContext { get; set; }

    /// <summary>True while files are uploading. Disable saving meanwhile, or the upload is not in the saved content.</summary>
    public bool IsUploading => uploads.Count > 0;

    private TipTapOptions TipTap => Services.GetService<TipTapOptions>() ?? DefaultOptions;
    private static readonly TipTapOptions DefaultOptions = new();
    internal RichTextEditorText Texts => Text ?? TipTap.Text;

    protected string InputId => UnmatchedParameters?.TryGetValue("id", out var id) == true && id is not null ? id.ToString()! : generatedId;
    private string LabelId => InputId + "-label";
    private string? NameValue => Name ?? MarkdownSyntax.FieldPath(ValueExpression);
    private bool Interactive => RendererInfo.IsInteractive;
    private bool Locked => Disabled || ReadOnly;
    private bool ToolbarVisible => Interactive && ShowToolbar && !ReadOnly;
    private bool UploadsEnabled => Interactive && UploadFile is not null && !Locked;
    private bool ImagesOnly => MarkdownSyntax.AcceptsOnlyImages(AcceptedFileTypes);
    private bool MentionsEnabled => Has(RichTextExtensions.Mention) && MentionSearch is not null;
    private bool Has(RichTextExtensions extension) => (EffectiveExtensions & extension) == extension;
    private bool IsMarkdown => Format == RichTextEditorFormat.Markdown;

    /// <summary>Features markdown cannot store; off in markdown format (underline too, which is part of Basic).</summary>
    private const RichTextExtensions NotInMarkdown = RichTextExtensions.Highlight | RichTextExtensions.TextAlign;
    private RichTextExtensions EffectiveExtensions => IsMarkdown ? Extensions & ~NotInMarkdown : Extensions;

    /// <summary>Whether a tool can work with the editor's features and settings.</summary>
    private bool Available(RichTextTool tool) => tool switch
    {
        RichTextTool.Separator => true,
        RichTextTool.Underline => Has(RichTextExtensions.Basic) && !IsMarkdown,
        RichTextTool.Highlight => Has(RichTextExtensions.Highlight),
        RichTextTool.TaskList => Has(RichTextExtensions.TaskList),
        RichTextTool.Table => Has(RichTextExtensions.Table),
        RichTextTool.AlignLeft or RichTextTool.AlignCenter or RichTextTool.AlignRight => Has(RichTextExtensions.TextAlign),
        RichTextTool.Upload => UploadsEnabled,
        RichTextTool.Undo or RichTextTool.Redo => true,
        _ => Has(RichTextExtensions.Basic)
    };

    /// <summary>The toolbar's groups of available tools, without empty groups. Duplicates are shown once.</summary>
    internal List<List<RichTextTool>> ToolbarGroups()
    {
        var groups = new List<List<RichTextTool>>();
        var current = new List<RichTextTool>();
        var seen = new HashSet<RichTextTool>();
        foreach (var tool in Toolbar ?? RichTextToolbar.Full)
        {
            if (tool == RichTextTool.Separator)
            {
                if (current.Count > 0)
                {
                    groups.Add(current);
                    current = [];
                }
            }
            else if (Enum.IsDefined(tool) && Available(tool) && seen.Add(tool))
            {
                current.Add(tool);
            }
        }
        if (current.Count > 0)
        {
            groups.Add(current);
        }
        return groups;
    }
    private string? AriaInvalid => fieldCssClasses.Contains("invalid", StringComparison.Ordinal) ? "true" : null;

    protected override string ClassNames => ClassBuilder
        .Add("rich-text-editor")
        .AddIf("rich-text-editor-disabled", Disabled)
        .AddIf("rich-text-editor-readonly", ReadOnly)
        .AddIf(fieldCssClasses, !string.IsNullOrEmpty(fieldCssClasses))
        .ToString();

    private string FrameClassNames => new ClassBuilder("form-control rich-text-editor-frame")
        .AddIf(fieldCssClasses, !string.IsNullOrEmpty(fieldCssClasses))
        .ToString();

    private string FrameStyle => string.Join(' ',
        MinHeight is null ? null : $"--rt-min-height: {MinHeight};",
        MaxHeight is null ? null : $"--rt-max-height: {MaxHeight};");

    // Options the browser editor can change in place; anything else in the setup key recreates the editor.
    private string ClientOptions => string.Join('|', DebounceMs ?? 0, Locked, Placeholder, AriaInvalid, SetupKey);

    private string SetupKey => string.Join('|', Format, (int)Extensions, MentionsEnabled, MaxLength,
        string.Join(',', CustomExtensions ?? []), System.Text.Json.JsonSerializer.Serialize(ExtensionOptions),
        MentionTrigger, MentionMinChars, MentionDebounceMs, UploadsEnabled);

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
            selfReference ??= DotNetObjectReference.Create(this);
            await module.InvokeVoidAsync("attach", root, CreateClientOptions(), selfReference);
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
    }

    private Task<IJSObjectReference> GetModuleAsync() =>
        moduleTask ??= JS.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask();

    private object CreateClientOptions()
    {
        var options = TipTap;
        return new
        {
            scriptUrl = options.ScriptUrl,
            extensionModules = options.ExtensionModules,
            setupKey = SetupKey,
            markdownScriptUrl = options.MarkdownScriptUrl,
            format = Format switch { RichTextEditorFormat.Json => "json", RichTextEditorFormat.Markdown => "markdown", _ => "html" },
            debounceMs = DebounceMs is > 0 ? DebounceMs.Value : 0,
            locked = Locked,
            placeholder = Placeholder,
            labelId = string.IsNullOrWhiteSpace(Label) ? null : LabelId,
            invalid = AriaInvalid is not null,
            maxLength = MaxLength,
            uploads = UploadsEnabled,
            extensions = new
            {
                basic = Has(RichTextExtensions.Basic),
                underline = Has(RichTextExtensions.Basic) && !IsMarkdown,
                image = Has(RichTextExtensions.Image),
                table = Has(RichTextExtensions.Table),
                taskList = Has(RichTextExtensions.TaskList),
                textAlign = Has(RichTextExtensions.TextAlign),
                highlight = Has(RichTextExtensions.Highlight),
                typography = Has(RichTextExtensions.Typography),
                mention = MentionsEnabled
            },
            customExtensions = CustomExtensions ?? [],
            extensionOptions = ExtensionOptions,
            mention = new { trigger = MentionTrigger.ToString(), minChars = Math.Max(0, MentionMinChars), debounceMs = Math.Max(0, MentionDebounceMs) },
            texts = new
            {
                noMentionMatches = Texts.NoMentionMatches,
                characterCount = Texts.CharacterCount,
                tableSize = Texts.TableSize
            }
        };
    }

    private async Task OnChangeAsync(ChangeEventArgs args)
    {
        var value = args.Value?.ToString();
        if (string.IsNullOrEmpty(value))
        {
            value = null;
        }
        if (value == currentValue)
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

    /// <summary>Moves the focus to the editor.</summary>
    public async Task FocusAsync()
    {
        if (module is not null && !disposed)
        {
            await module.InvokeVoidAsync("focus", root);
        }
    }

    /// <summary>
    /// Inserts content at the caret, replacing the selection, as one undoable edit. HTML is parsed with the editor's
    /// features, so markup they do not support is dropped. Does nothing before the editor is interactive.
    /// </summary>
    public async Task InsertContentAsync(string html)
    {
        if (module is not null && !disposed && !string.IsNullOrEmpty(html))
        {
            await module.InvokeVoidAsync("insertContent", root, html);
        }
    }

    /// <summary>
    /// Runs a TipTap command, such as <c>"toggleBold"</c> or <c>"setTextAlign"</c> with <c>"center"</c>, and returns
    /// whether it ran. Does nothing before the editor is interactive.
    /// </summary>
    public async Task<bool> RunCommandAsync(string command, object? args = null)
    {
        if (module is null || disposed || string.IsNullOrWhiteSpace(command))
        {
            return false;
        }
        return await module.InvokeAsync<bool>("runCommand", root, command, args);
    }

    /// <summary>Called by the browser editor while the user types a mention. Cancels the previous search.</summary>
    [JSInvokable]
    public async Task<IReadOnlyList<RichTextMention>> SearchMentionsAsync(string query)
    {
        if (MentionSearch is null || disposed)
        {
            return [];
        }

        var search = new CancellationTokenSource();
        CancelQuietly(Interlocked.Exchange(ref mentionSearch, search));
        try
        {
            var results = await MentionSearch(query ?? "", search.Token);
            return results ?? [];
        }
        catch (OperationCanceledException) when (search.IsCancellationRequested)
        {
            return [];
        }
        finally
        {
            Interlocked.CompareExchange(ref mentionSearch, null, search);
            search.Dispose();
        }
    }

    private static void CancelQuietly(CancellationTokenSource? source)
    {
        try
        {
            source?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The search already finished.
        }
    }

    private async Task OnFilesSelectedAsync(InputFileChangeEventArgs args)
    {
        if (UploadFile is null || module is null || disposed)
        {
            return;
        }

        var started = new List<(EditorUpload Upload, IBrowserFile File)>();
        foreach (var file in args.GetMultipleFiles(Math.Max(1, args.FileCount)))
        {
            if (!MarkdownSyntax.Accepts(AcceptedFileTypes, file.Name, file.ContentType))
            {
                await ReportErrorAsync(new EditorUploadError(file.Name, string.Format(Texts.FileTypeNotAccepted, file.Name), null));
            }
            else if (file.Size > MaxFileSize)
            {
                await ReportErrorAsync(new EditorUploadError(file.Name,
                    string.Format(Texts.FileTooLarge, file.Name, MarkdownSyntax.FormatSize(MaxFileSize)), null));
            }
            else
            {
                started.Add((new EditorUpload(Guid.NewGuid().ToString("N")[..12], file.Name, file.Size), file));
            }
        }

        if (started.Count == 0)
        {
            return;
        }

        await module.InvokeVoidAsync("startUploads", root,
            started.Select(s => new { token = s.Upload.Token, label = string.Format(Texts.Uploading, s.Upload.FileName) }));

        uploads.AddRange(started.Select(s => s.Upload));
        await NotifyUploadStateAsync();

        foreach (var (upload, file) in started)
        {
            _ = RunUploadAsync(upload, file);
        }
    }

    private async Task RunUploadAsync(EditorUpload upload, IBrowserFile browserFile)
    {
        try
        {
            var file = EditorUploadFile.FromBrowserFile(browserFile, MaxFileSize);
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

            var embed = (result.Embed ?? MarkdownSyntax.IsEmbeddableImage(file.ContentType)) && Has(RichTextExtensions.Image);
            var text = result.Text ?? (embed ? Path.GetFileNameWithoutExtension(file.Name) : file.Name);
            var inserted = await FinishUploadAsync(upload, new { url = result.Url, text, embed });
            if (!inserted && !disposed)
            {
                await OnUploadDiscarded.InvokeAsync(result);
            }
        }
        catch (OperationCanceledException) when (upload.Cancellation.IsCancellationRequested)
        {
            await FinishUploadAsync(upload, null);
        }
        catch (EditorUploadException exception)
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
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private async Task FailAsync(EditorUpload upload, string message, Exception exception)
    {
        await FinishUploadAsync(upload, null);
        if (!disposed)
        {
            await ReportErrorAsync(new EditorUploadError(upload.FileName, message, exception));
        }
    }

    /// <summary>Replaces the upload's placeholder with the file (or removes it when <paramref name="content"/> is null).</summary>
    private async Task<bool> FinishUploadAsync(EditorUpload upload, object? content)
    {
        if (module is null || disposed)
        {
            return false;
        }

        try
        {
            return await module.InvokeAsync<bool>("finishUpload", root, upload.Token, content);
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

    private async Task ReportErrorAsync(EditorUploadError error)
    {
        uploadErrors.Add(error);
        await OnUploadError.InvokeAsync(error);
    }

    private void DismissError(EditorUploadError error) => uploadErrors.Remove(error);

    private void CancelUpload(EditorUpload upload) => upload.Cancellation.Cancel();

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

    /// <summary>
    /// What static rendering shows: HTML when it may be shown, markdown through the app's <see cref="IMarkdownRenderer"/>,
    /// otherwise the text.
    /// </summary>
    private RenderFragment StaticContent() => builder =>
    {
        if (string.IsNullOrWhiteSpace(currentValue))
        {
            return;
        }

        switch (Format)
        {
            case RichTextEditorFormat.Html when TipTap.SafeHtml(currentValue) is { } html:
                builder.AddMarkupContent(0, html);
                break;
            case RichTextEditorFormat.Html:
                builder.AddContent(1, PlainText(currentValue));
                break;
            case RichTextEditorFormat.Markdown when Services.GetService<IMarkdownRenderer>() is { } renderer:
                builder.AddContent(2, renderer.Render(currentValue));
                break;
            case RichTextEditorFormat.Markdown:
                builder.OpenElement(3, "div");
                builder.AddAttribute(4, "style", "white-space: pre-wrap");
                builder.AddContent(5, currentValue);
                builder.CloseElement();
                break;
        }
    };

    internal static string PlainText(string html) =>
        WebUtility.HtmlDecode(Tags().Replace(BlockEnds().Replace(html, "\n"), "")).Trim();

    [GeneratedRegex(@"<[^>]*>")]
    private static partial Regex Tags();

    [GeneratedRegex(@"</(p|h[1-6]|li|blockquote|pre|tr)>|<br\s*/?>", RegexOptions.IgnoreCase)]
    private static partial Regex BlockEnds();

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

        CancelQuietly(mentionSearch);
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

        selfReference?.Dispose();
        GC.SuppressFinalize(this);
    }
}
