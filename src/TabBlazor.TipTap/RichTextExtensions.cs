namespace TabBlazor;

/// <summary>
/// The built-in features a <see cref="RichTextEditor"/> turns on. Combine them with <c>|</c>, for example
/// <c>RichTextExtensions.Default | RichTextExtensions.Table</c>. Content a turned-off feature would hold, such as a
/// pasted table without <see cref="Table"/>, is reduced to plain paragraphs.
/// </summary>
[Flags]
public enum RichTextExtensions
{
    /// <summary>Paragraphs only.</summary>
    None = 0,

    /// <summary>
    /// Headings (H1–H3), bold, italic, underline, strike, inline code, bulleted and numbered lists, quotes, code blocks,
    /// horizontal rules, links and undo/redo.
    /// </summary>
    Basic = 1 << 0,

    /// <summary>Images. They are only added through uploads (<c>UploadFile</c>), never stored inline as base64.</summary>
    Image = 1 << 1,

    /// <summary>Tables, with a size picker and row and column tools.</summary>
    Table = 1 << 2,

    /// <summary>Task lists with check boxes.</summary>
    TaskList = 1 << 3,

    /// <summary>Left, center and right alignment of paragraphs and headings.</summary>
    TextAlign = 1 << 4,

    /// <summary>Highlighted (marked) text.</summary>
    Highlight = 1 << 5,

    /// <summary>Smart quotes, dashes and ellipses while typing.</summary>
    Typography = 1 << 6,

    /// <summary><c>@</c> mentions. Needs <c>MentionSearch</c>.</summary>
    Mention = 1 << 7,

    /// <summary><see cref="Basic"/> plus <see cref="Image"/>, the default.</summary>
    Default = Basic | Image,

    /// <summary>Every built-in feature except <see cref="Mention"/>, which needs a search callback.</summary>
    All = Basic | Image | Table | TaskList | TextAlign | Highlight | Typography
}
