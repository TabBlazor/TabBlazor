namespace TabBlazor;

/// <summary>What a <see cref="RichTextEditor"/>'s <c>Value</c> holds.</summary>
public enum RichTextEditorFormat
{
    /// <summary>HTML, such as <c>&lt;p&gt;Hello &lt;strong&gt;world&lt;/strong&gt;&lt;/p&gt;</c>. Sanitize it before showing it to other users.</summary>
    Html,

    /// <summary>The TipTap (ProseMirror) document as a JSON string. Lossless, and carries no markup to sanitize.</summary>
    Json,

    /// <summary>
    /// Markdown (CommonMark with GitHub tables and task lists), edited WYSIWYG. Features markdown cannot store are turned
    /// off: underline, highlight and alignment. Mentions are stored as <c>[@ id="42" label="Anna"]</c>. App extensions
    /// need their own markdown rules, or their content is lost when the value is saved.
    /// </summary>
    Markdown
}
