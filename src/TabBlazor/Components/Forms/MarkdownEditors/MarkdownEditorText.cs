#nullable enable
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Localization;

namespace TabBlazor;

/// <summary>
/// The built-in UI strings of a <see cref="MarkdownEditor"/>, with English defaults. Override strings per editor
/// with <c>Text="@(new MarkdownEditorText { Bold = "Fet" })"</c>, for the whole app with
/// <see cref="TablerOptions.MarkdownEditorText"/>, or translate them with a resource through
/// <see cref="TablerOptions.MarkdownEditorTextResource"/> (see <see cref="FromLocalizer"/>).
/// </summary>
/// <remarks>
/// Toolbar tooltips append the keyboard shortcut, for example "Bold (Ctrl+B)"; on macOS the editor shows ⌘ instead
/// of Ctrl.
/// </remarks>
public sealed record MarkdownEditorText
{
    private static readonly PropertyInfo[] StringProperties = typeof(MarkdownEditorText)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.PropertyType == typeof(string) && p.CanWrite)
        .ToArray();

    private static readonly ConcurrentDictionary<(Type Resource, string Culture, MarkdownEditorText Fallback), MarkdownEditorText> Cache = new();

    public static MarkdownEditorText Default { get; } = new();

    /// <summary>
    /// Builds the strings from a localizer. The keys are the property names (<c>Bold</c>, <c>Uploading</c>, …);
    /// a key the resource does not have keeps the <paramref name="fallback"/> string (English by default).
    /// Formatted strings keep their placeholders, for example <c>"Laddar upp {0}…"</c>.
    /// </summary>
    public static MarkdownEditorText FromLocalizer(IStringLocalizer localizer, MarkdownEditorText? fallback = null)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        var text = (fallback ?? Default) with { };
        foreach (var property in StringProperties)
        {
            var value = localizer[property.Name];
            if (!value.ResourceNotFound && !string.IsNullOrEmpty(value.Value))
            {
                property.SetValue(text, value.Value);
            }
        }
        return text;
    }

    /// <summary>The strings of a resource for the current UI culture, cached per culture.</summary>
    internal static MarkdownEditorText FromResource(IStringLocalizerFactory factory, Type resource, MarkdownEditorText fallback) =>
        Cache.GetOrAdd((resource, CultureInfo.CurrentUICulture.Name, fallback),
            key => FromLocalizer(factory.Create(key.Resource), key.Fallback));

    public string Toolbar { get; init; } = "Formatting";
    public string Heading { get; init; } = "Heading";
    public string Heading1 { get; init; } = "Heading 1";
    public string Heading2 { get; init; } = "Heading 2";
    public string Heading3 { get; init; } = "Heading 3";
    public string NormalText { get; init; } = "Normal text";
    public string Bold { get; init; } = "Bold";
    public string Italic { get; init; } = "Italic";
    public string BulletedList { get; init; } = "Bulleted list";
    public string NumberedList { get; init; } = "Numbered list";
    public string Quote { get; init; } = "Quote";
    public string Code { get; init; } = "Code";
    public string Link { get; init; } = "Link";
    public string Image { get; init; } = "Image";
    public string Table { get; init; } = "Table";

    /// <summary>Names a size in the table picker. Arguments: the number of columns and of rows, header row included.</summary>
    public string TableSize { get; init; } = "{0} × {1} table";

    /// <summary>The header of each column in an inserted table. Argument: the column number.</summary>
    public string TableColumn { get; init; } = "Column {0}";
    public string AttachFiles { get; init; } = "Attach files";
    public string AttachImages { get; init; } = "Add images";
    public string LinkTextPlaceholder { get; init; } = "link text";
    public string ImageTextPlaceholder { get; init; } = "image description";
    public string Mode { get; init; } = "View";
    public string Write { get; init; } = "Write";
    public string Hybrid { get; init; } = "Hybrid";
    public string Preview { get; init; } = "Preview";
    public string Split { get; init; } = "Side by side";
    public string NothingToPreview { get; init; } = "Nothing to preview";
    public string Cancel { get; init; } = "Cancel";
    public string Dismiss { get; init; } = "Dismiss";

    /// <summary>The placeholder text and status while a file uploads. Argument: the file name.</summary>
    public string Uploading { get; init; } = "Uploading {0}…";

    /// <summary>Shown when an upload fails. Argument: the file name.</summary>
    public string UploadFailed { get; init; } = "Could not upload {0}.";

    /// <summary>Shown when a file exceeds the size limit. Arguments: the file name and the limit, for example "10 MB".</summary>
    public string FileTooLarge { get; init; } = "{0} is larger than {1}.";

    /// <summary>Shown when a file type is not accepted. Argument: the file name.</summary>
    public string FileTypeNotAccepted { get; init; } = "{0} is not an accepted file type.";
}
