using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Components;

namespace TabBlazor;

/// <summary>
/// Renders markdown with <see href="https://github.com/xoofx/markdig">Markdig</see> for the <see cref="MarkdownEditor"/>
/// preview, or anywhere else markdown from users is shown. Safe by default: raw HTML is shown as text and links and
/// images may only use the URL schemes in <see cref="MarkdownRendererOptions"/>. Thread-safe; register it once.
/// </summary>
public sealed class MarkdigMarkdownRenderer : IMarkdownRenderer
{
    private readonly MarkdownPipeline pipeline;
    private readonly HashSet<string> linkSchemes;
    private readonly HashSet<string> imageSchemes;
    private readonly Func<string, string>? sanitizer;

    public MarkdigMarkdownRenderer(MarkdownRendererOptions? options = null)
    {
        options ??= new MarkdownRendererOptions();
        linkSchemes = new HashSet<string>(options.AllowedUrlSchemes, StringComparer.OrdinalIgnoreCase);
        imageSchemes = new HashSet<string>(options.AllowedImageUrlSchemes, StringComparer.OrdinalIgnoreCase);
        sanitizer = options.Sanitizer;

        var builder = new MarkdownPipelineBuilder()
            .UsePipeTables()
            .UseTaskLists()
            .UseEmphasisExtras(Markdig.Extensions.EmphasisExtras.EmphasisExtraOptions.Strikethrough)
            .UseAutoLinks();

        if (options.SoftLineBreaksAsHardLineBreaks)
        {
            builder.UseSoftlineBreakAsHardlineBreak();
        }

        if (sanitizer is null)
        {
            builder.DisableHtml();
        }

        options.ConfigurePipeline?.Invoke(builder);
        pipeline = builder.Build();
    }

    /// <inheritdoc />
    public MarkupString Render(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return new MarkupString("");
        }

        var document = Markdown.Parse(markdown, pipeline);
        RestrictUrls(document);

        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer);
        pipeline.Setup(renderer);
        renderer.Render(document);
        writer.Flush();

        var html = writer.ToString();
        return new MarkupString(sanitizer is null ? html : sanitizer(html));
    }

    private void RestrictUrls(MarkdownDocument document)
    {
        foreach (var link in document.Descendants<LinkInline>())
        {
            if (link.IsImage)
            {
                if (!IsAllowed(link.Url, imageSchemes))
                {
                    link.Url = "";
                }
            }
            else if (!IsAllowed(link.Url, linkSchemes))
            {
                link.Url = "#";
            }
        }

        foreach (var autolink in document.Descendants<AutolinkInline>().ToList())
        {
            var schemes = autolink.IsEmail ? linkSchemes.Contains("mailto") : IsAllowed(autolink.Url, linkSchemes);
            if (!schemes)
            {
                autolink.ReplaceBy(new LiteralInline(autolink.Url));
            }
        }
    }

    /// <summary>
    /// Whether a URL is relative or uses one of <paramref name="schemes"/>. Ignores the whitespace and control
    /// characters browsers ignore, so <c>java&#9;script:</c> is caught.
    /// </summary>
    internal static bool IsAllowed(string? url, ISet<string> schemes)
    {
        if (string.IsNullOrEmpty(url))
        {
            return true;
        }

        var cleaned = new string(url.Where(c => c > ' ' && c != '\u007f').ToArray());
        var colon = cleaned.IndexOf(':');
        if (colon < 0)
        {
            return true;
        }

        var pathStart = cleaned.IndexOfAny(['/', '?', '#']);
        if (pathStart >= 0 && pathStart < colon)
        {
            // "folder/file:name" is a relative path, not a scheme.
            return true;
        }

        return schemes.Contains(cleaned[..colon]);
    }
}
