using Markdig;

namespace TabBlazor;

/// <summary>Options for <see cref="MarkdigMarkdownRenderer"/>.</summary>
public sealed class MarkdownRendererOptions
{
    /// <summary>
    /// URL schemes links may use. Defaults to <c>http</c>, <c>https</c> and <c>mailto</c>. Relative URLs are always
    /// allowed. A link with another scheme (such as <c>javascript:</c>) points to <c>#</c> instead.
    /// </summary>
    public ISet<string> AllowedUrlSchemes { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "http", "https", "mailto" };

    /// <summary>
    /// URL schemes images may use. Defaults to <c>http</c> and <c>https</c>. Relative URLs are always allowed. Add
    /// <c>data</c> to show images stored inline as data URLs. An image with another scheme renders without a source.
    /// </summary>
    public ISet<string> AllowedImageUrlSchemes { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "http", "https" };

    /// <summary>Renders single line breaks as line breaks, like comments on GitHub. Defaults to false (CommonMark).</summary>
    public bool SoftLineBreaksAsHardLineBreaks { get; set; }

    /// <summary>
    /// Adds Markdig extensions to the default pipeline (CommonMark plus pipe tables, task lists, strikethrough and
    /// automatic links). Extensions that emit raw attributes or HTML, such as generic attributes, can make the output
    /// unsafe; with those, render through <see cref="AllowHtml"/> and a sanitizer.
    /// </summary>
    public Action<MarkdownPipelineBuilder>? ConfigurePipeline { get; set; }

    internal Func<string, string>? Sanitizer { get; private set; }

    /// <summary>
    /// Lets raw HTML in the markdown through, after passing the rendered HTML to <paramref name="sanitize"/>, which
    /// must remove anything unsafe. For example, with the HtmlSanitizer package:
    /// <code>options.AllowHtml(new Ganss.Xss.HtmlSanitizer().Sanitize);</code>
    /// Without this, raw HTML is shown as text.
    /// </summary>
    public MarkdownRendererOptions AllowHtml(Func<string, string> sanitize)
    {
        Sanitizer = sanitize ?? throw new ArgumentNullException(nameof(sanitize),
            "Raw HTML needs a sanitizer; without one the preview could run script from the markdown.");
        return this;
    }
}
