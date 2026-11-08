namespace TabBlazor;

/// <summary>A button (or the divider between groups) in a <see cref="RichTextEditor"/>'s toolbar.</summary>
public enum RichTextTool
{
    /// <summary>A divider: starts a new group of buttons.</summary>
    Separator,
    /// <summary>The Normal text / Heading 1–3 menu.</summary>
    TextStyle,
    Bold,
    Italic,
    Underline,
    Strike,
    /// <summary>Inline code.</summary>
    Code,
    /// <summary>Needs <see cref="RichTextExtensions.Highlight"/>.</summary>
    Highlight,
    BulletList,
    NumberedList,
    /// <summary>Needs <see cref="RichTextExtensions.TaskList"/>.</summary>
    TaskList,
    Quote,
    CodeBlock,
    Link,
    /// <summary>Adds images or files. Shown only when <c>UploadFile</c> is set.</summary>
    Upload,
    /// <summary>The table size picker. Needs <see cref="RichTextExtensions.Table"/>; the row and column tools appear next to it while the caret is in a table.</summary>
    Table,
    HorizontalRule,
    /// <summary>Needs <see cref="RichTextExtensions.TextAlign"/>.</summary>
    AlignLeft,
    /// <summary>Needs <see cref="RichTextExtensions.TextAlign"/>.</summary>
    AlignCenter,
    /// <summary>Needs <see cref="RichTextExtensions.TextAlign"/>.</summary>
    AlignRight,
    Undo,
    Redo
}

/// <summary>
/// Ready-made toolbars for <see cref="RichTextEditor.Toolbar"/>. Write your own list to pick buttons and their order;
/// <see cref="RichTextTool.Separator"/> starts a new group.
/// </summary>
public static class RichTextToolbar
{
    /// <summary>The essentials: bold, italic, lists and link.</summary>
    public static IReadOnlyList<RichTextTool> Minimal { get; } =
    [
        RichTextTool.Bold, RichTextTool.Italic,
        RichTextTool.Separator,
        RichTextTool.BulletList, RichTextTool.NumberedList,
        RichTextTool.Separator,
        RichTextTool.Link
    ];

    /// <summary>Every button, the default. Buttons whose feature is off are left out.</summary>
    public static IReadOnlyList<RichTextTool> Full { get; } =
    [
        RichTextTool.TextStyle,
        RichTextTool.Separator,
        RichTextTool.Bold, RichTextTool.Italic, RichTextTool.Underline, RichTextTool.Strike, RichTextTool.Code, RichTextTool.Highlight,
        RichTextTool.Separator,
        RichTextTool.BulletList, RichTextTool.NumberedList, RichTextTool.TaskList, RichTextTool.Quote, RichTextTool.CodeBlock,
        RichTextTool.Separator,
        RichTextTool.Link, RichTextTool.Upload, RichTextTool.Table, RichTextTool.HorizontalRule,
        RichTextTool.Separator,
        RichTextTool.AlignLeft, RichTextTool.AlignCenter, RichTextTool.AlignRight,
        RichTextTool.Separator,
        RichTextTool.Undo, RichTextTool.Redo
    ];
}
