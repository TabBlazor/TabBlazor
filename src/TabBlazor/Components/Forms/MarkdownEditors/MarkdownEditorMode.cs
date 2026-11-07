namespace TabBlazor;

/// <summary>What a <see cref="MarkdownEditor"/> shows.</summary>
public enum MarkdownEditorMode
{
    /// <summary>Only the text area.</summary>
    Edit,
    /// <summary>Only the rendered preview.</summary>
    Preview,
    /// <summary>The text area and the preview side by side; stacked on narrow screens.</summary>
    Split,
    /// <summary>
    /// The text area with the markdown styled as you type: bold looks bold, headings, links and code stand out and
    /// the markers are dimmed. The text is still markdown, set in the monospace font. Needs no renderer.
    /// </summary>
    Hybrid
}
