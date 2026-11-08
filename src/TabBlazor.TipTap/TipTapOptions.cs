namespace TabBlazor;

/// <summary>App-wide settings for <see cref="RichTextEditor"/> and <see cref="RichTextView"/>, set with <c>AddTabBlazorTipTap</c>.</summary>
public sealed class TipTapOptions
{
    /// <summary>The default for <see cref="ScriptUrl"/>.</summary>
    public const string DefaultScriptUrl = "./_content/TabBlazor.TipTap/tiptap.js";

    /// <summary>
    /// Where the TipTap bundle is loaded from. Defaults to the bundle in this package. Set it to serve your own build,
    /// for example one with more extensions; it must export the same names as the package's bundle.
    /// </summary>
    public string ScriptUrl { get; set; } = DefaultScriptUrl;

    /// <summary>The default for <see cref="MarkdownScriptUrl"/>.</summary>
    public const string DefaultMarkdownScriptUrl = "./_content/TabBlazor.TipTap/tiptap-markdown.js";

    /// <summary>
    /// Where the markdown support is loaded from, only by editors with <see cref="RichTextEditorFormat.Markdown"/>.
    /// Not used when the bundle at <see cref="ScriptUrl"/> exports <c>Markdown</c> itself.
    /// </summary>
    public string MarkdownScriptUrl { get; set; } = DefaultMarkdownScriptUrl;

    /// <summary>
    /// ES modules with app-defined TipTap extensions, loaded once before the first editor starts. Each exports
    /// <c>register(registry)</c> and calls <c>registry.add(name, options =&gt; extension)</c>; editors turn them on by
    /// name with <see cref="RichTextEditor.CustomExtensions"/>. Import TipTap from <see cref="ScriptUrl"/>, never from npm.
    /// </summary>
    public IList<string> ExtensionModules { get; } = [];

    /// <summary>The toolbar and message texts. <see cref="RichTextEditor.Text"/> overrides it per editor.</summary>
    public RichTextEditorText Text { get; set; } = RichTextEditorText.Default;

    /// <summary>
    /// Shows HTML without a sanitizer, for apps that sanitize when they save. Without this or <see cref="AllowHtml"/>,
    /// <see cref="RichTextView"/> throws and the read-only editor in static rendering shows plain text.
    /// </summary>
    public bool AllowUnsanitizedHtml { get; set; }

    internal Func<string, string>? Sanitizer { get; private set; }

    /// <summary>
    /// Passes HTML through <paramref name="sanitize"/> before <see cref="RichTextView"/> or the read-only editor shows it.
    /// It must remove anything unsafe. For example, with the HtmlSanitizer package:
    /// <code>options.AllowHtml(new Ganss.Xss.HtmlSanitizer().Sanitize);</code>
    /// This protects the display only: also sanitize where the HTML is saved, since a browser can post anything.
    /// </summary>
    public TipTapOptions AllowHtml(Func<string, string> sanitize)
    {
        Sanitizer = sanitize ?? throw new ArgumentNullException(nameof(sanitize));
        return this;
    }

    /// <summary>The HTML to show, or null when it may not be shown as HTML.</summary>
    internal string? SafeHtml(string? html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return html;
        }
        if (Sanitizer is { } sanitize)
        {
            return sanitize(html);
        }
        return AllowUnsanitizedHtml ? html : null;
    }
}
