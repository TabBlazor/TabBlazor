#nullable enable
using Microsoft.AspNetCore.Components;

namespace TabBlazor;

/// <summary>
/// Turns markdown into HTML for the preview of a <see cref="MarkdownEditor"/>. Register one in DI
/// (for example with <c>services.AddTabBlazorMarkdown()</c> from the <c>TabBlazor.Markdown</c> package)
/// or pass it to the editor's <c>Renderer</c> parameter. Without one the editor has no preview.
/// </summary>
/// <remarks>
/// The editor injects the returned markup as-is, so an implementation must return HTML that is safe to
/// render: no script, no event handler attributes and no <c>javascript:</c> URLs, whatever the markdown says.
/// </remarks>
public interface IMarkdownRenderer
{
    /// <summary>Renders <paramref name="markdown"/> to safe HTML. Returns empty markup for null or empty input.</summary>
    MarkupString Render(string? markdown);
}
