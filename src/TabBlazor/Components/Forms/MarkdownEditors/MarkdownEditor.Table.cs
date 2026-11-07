#nullable enable
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace TabBlazor;

/// <summary>Where the table being edited in the dialog came from, as the editor's script read it.</summary>
internal sealed class MarkdownTableSource
{
    /// <summary>False when the caret is not in a table: saving inserts a new one at the caret.</summary>
    public bool Found { get; set; }
    public int Start { get; set; }
    public int End { get; set; }
    /// <summary>The table's text when it was read, to find it again if the text around it changed meanwhile.</summary>
    public string? Text { get; set; }
    public string Indent { get; set; } = "";
    /// <summary>Header, separator row and body rows, as markdown cell text.</summary>
    public string[][] Rows { get; set; } = [];
}

// The table dialog: edits the table at the caret (or a new one) in a grid and writes it back as one undoable edit.
public partial class MarkdownEditor
{
    private static readonly ModalOptions TableDialogOptions = new()
    {
        Size = ModalSize.Large,
        Fullscreen = ModalFullscreen.BelowMedium,
        CloseOnEsc = true
    };

    private MarkdownTable? tableEdit;
    private MarkdownTableSource? tableSource;
    private bool focusTableEditor;

    private bool TableDialogOpen => tableEdit is not null;

    /// <summary>Opens the table dialog for the table at the caret, or for a new table when the caret is not in one.</summary>
    private async Task OpenTableEditorAsync()
    {
        if (module is null || disposed || FormattingDisabled)
        {
            return;
        }

        var source = await module.InvokeAsync<MarkdownTableSource?>("readTable", root);
        if (source is null || disposed)
        {
            return;
        }

        tableSource = source;
        tableEdit = source.Found && source.Rows.Length > 0
            ? MarkdownTable.FromCells(source.Rows)
            : MarkdownTable.Create(3, 2, Texts.TableColumn);
        focusTableEditor = true;
    }

    private async Task SaveTableAsync()
    {
        if (tableEdit is null || tableSource is null || module is null || disposed)
        {
            return;
        }

        var payload = new
        {
            found = tableSource.Found,
            start = tableSource.Start,
            end = tableSource.End,
            text = tableSource.Text,
            indent = tableSource.Indent,
            rows = tableEdit.ToCells()
        };
        tableEdit = null;
        tableSource = null;
        await module.InvokeVoidAsync("writeTable", root, payload);
    }

    private async Task CloseTableEditorAsync()
    {
        tableEdit = null;
        tableSource = null;
        await FocusAsync();
    }

    private static void SetCell(List<string> row, int column, ChangeEventArgs args) =>
        row[column] = MarkdownTable.CellText(args.Value?.ToString());

    private void ToggleAlignment(int column, MarkdownTableAlignment alignment)
    {
        if (tableEdit is not null)
        {
            tableEdit.Alignments[column] = tableEdit.Alignments[column] == alignment ? MarkdownTableAlignment.None : alignment;
        }
    }

    private sealed record TablePaste(int Row, int Column, string? Text);

    /// <summary>The script sends spreadsheet text pasted into a cell as JSON through a hidden input.</summary>
    private void OnTablePaste(ChangeEventArgs args)
    {
        TablePaste? paste;
        try
        {
            paste = JsonSerializer.Deserialize<TablePaste>(args.Value?.ToString() ?? "", JsonSerializerOptions.Web);
        }
        catch (JsonException)
        {
            return;
        }

        if (tableEdit is not null && paste?.Text is { } text)
        {
            tableEdit.Paste(Math.Clamp(paste.Row, 0, tableEdit.Rows.Count), Math.Clamp(paste.Column, 0, tableEdit.Columns), text);
        }
    }

    private static string AlignmentStyle(MarkdownTableAlignment alignment) => alignment switch
    {
        MarkdownTableAlignment.Center => "text-align: center",
        MarkdownTableAlignment.Right => "text-align: right",
        _ => ""
    };
}
