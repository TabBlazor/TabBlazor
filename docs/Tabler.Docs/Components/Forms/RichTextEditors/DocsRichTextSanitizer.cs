using Ganss.Xss;

namespace Tabler.Docs.Components.Forms.RichTextEditors;

/// <summary>
/// An HtmlSanitizer set up for the HTML RichTextEditor writes: its default rules plus the attributes of mentions, task
/// lists, alignment and the docs' callout extension. Copy it and remove what your editors do not use.
/// </summary>
public static class DocsRichTextSanitizer
{
    public static HtmlSanitizer Create()
    {
        var sanitizer = new HtmlSanitizer();

        // Images are uploads with http(s) or relative URLs; the docs keep them as data URLs because they have no server.
        sanitizer.AllowedSchemes.Add("mailto");
        sanitizer.AllowedSchemes.Add("data");

        // Mentions: <span data-type="mention" data-id="42" data-label="Anna">@Anna</span>
        // Task lists: <ul data-type="taskList"><li data-type="taskItem" data-checked="true"><label><input type="checkbox" checked>…
        foreach (var attribute in new[] { "data-type", "data-id", "data-label", "data-mention-suggestion-char", "data-checked", "data-callout" })
        {
            sanitizer.AllowedAttributes.Add(attribute);
        }
        sanitizer.AllowedTags.Add("input");
        sanitizer.AllowedTags.Add("label");
        sanitizer.AllowedAttributes.Add("type");
        sanitizer.AllowedAttributes.Add("checked");
        sanitizer.AllowedAttributes.Add("disabled");

        // Links get rel="noopener noreferrer nofollow" from the editor; keep it.
        sanitizer.AllowedAttributes.Add("rel");

        // Text alignment is an inline style.
        sanitizer.AllowedCssProperties.Clear();
        sanitizer.AllowedCssProperties.Add("text-align");
        return sanitizer;
    }
}
