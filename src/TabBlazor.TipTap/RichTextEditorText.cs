namespace TabBlazor;

/// <summary>
/// The texts a <see cref="RichTextEditor"/> shows: toolbar labels, upload messages and the mention list. Override some
/// with <c>RichTextEditorText.Default with { Bold = "Fet" }</c>, per editor (<see cref="RichTextEditor.Text"/>) or for
/// the app (<see cref="TipTapOptions.Text"/>).
/// </summary>
public sealed record RichTextEditorText
{
    /// <summary>The English texts.</summary>
    public static RichTextEditorText Default { get; } = new();

    public string Toolbar { get; init; } = "Formatting";
    public string TextStyle { get; init; } = "Text style";
    public string NormalText { get; init; } = "Normal text";
    public string Heading1 { get; init; } = "Heading 1";
    public string Heading2 { get; init; } = "Heading 2";
    public string Heading3 { get; init; } = "Heading 3";
    public string Bold { get; init; } = "Bold";
    public string Italic { get; init; } = "Italic";
    public string Underline { get; init; } = "Underline";
    public string Strike { get; init; } = "Strikethrough";
    public string Code { get; init; } = "Inline code";
    public string Highlight { get; init; } = "Highlight";
    public string BulletedList { get; init; } = "Bulleted list";
    public string NumberedList { get; init; } = "Numbered list";
    public string TaskList { get; init; } = "Task list";
    public string Quote { get; init; } = "Quote";
    public string CodeBlock { get; init; } = "Code block";
    public string HorizontalRule { get; init; } = "Horizontal line";
    public string Link { get; init; } = "Link";
    public string LinkUrl { get; init; } = "Link address";
    public string Apply { get; init; } = "Apply";
    public string RemoveLink { get; init; } = "Remove link";
    public string AttachImages { get; init; } = "Add images";
    public string AttachFiles { get; init; } = "Attach files";
    public string Table { get; init; } = "Table";
    /// <summary>The size under the table picker. {0} is the number of columns, {1} of rows.</summary>
    public string TableSize { get; init; } = "{0} × {1}";
    public string AddRow { get; init; } = "Add row below";
    public string AddColumn { get; init; } = "Add column to the right";
    public string DeleteRow { get; init; } = "Delete row";
    public string DeleteColumn { get; init; } = "Delete column";
    public string DeleteTable { get; init; } = "Delete table";
    public string AlignLeft { get; init; } = "Align left";
    public string AlignCenter { get; init; } = "Center";
    public string AlignRight { get; init; } = "Align right";
    public string Undo { get; init; } = "Undo";
    public string Redo { get; init; } = "Redo";

    /// <summary>{0} is the file name.</summary>
    public string Uploading { get; init; } = "Uploading {0}…";
    public string Cancel { get; init; } = "Cancel";
    public string Dismiss { get; init; } = "Dismiss";
    /// <summary>{0} is the file name.</summary>
    public string FileTypeNotAccepted { get; init; } = "{0} is not a file type you can add here.";
    /// <summary>{0} is the file name, {1} the size limit.</summary>
    public string FileTooLarge { get; init; } = "{0} is larger than {1}.";
    /// <summary>{0} is the file name.</summary>
    public string UploadFailed { get; init; } = "Could not upload {0}.";

    public string NoMentionMatches { get; init; } = "No matches";
    /// <summary>The counter under the editor with <c>MaxLength</c>. {0} is the number of characters, {1} the limit.</summary>
    public string CharacterCount { get; init; } = "{0} / {1}";
}
