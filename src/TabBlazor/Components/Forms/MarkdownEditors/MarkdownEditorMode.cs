namespace TabBlazor;

/// <summary>What a <see cref="MarkdownEditor"/> shows.</summary>
public enum MarkdownEditorMode
{
    /// <summary>Only the text area.</summary>
    Edit,
    /// <summary>Only the rendered preview.</summary>
    Preview,
    /// <summary>The text area and the preview side by side; stacked on narrow screens.</summary>
    Split
}
