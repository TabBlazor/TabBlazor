namespace TabBlazor;

/// <summary>What a <see cref="RichTextEditor"/>'s <c>Value</c> holds.</summary>
public enum RichTextEditorFormat
{
    /// <summary>HTML, such as <c>&lt;p&gt;Hello &lt;strong&gt;world&lt;/strong&gt;&lt;/p&gt;</c>. Sanitize it before showing it to other users.</summary>
    Html,

    /// <summary>The TipTap (ProseMirror) document as a JSON string. Lossless, and carries no markup to sanitize.</summary>
    Json
}
