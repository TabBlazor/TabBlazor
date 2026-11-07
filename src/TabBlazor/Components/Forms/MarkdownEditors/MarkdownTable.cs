#nullable enable
using System.Text;

namespace TabBlazor;

internal enum MarkdownTableAlignment
{
    None,
    Left,
    Center,
    Right
}

/// <summary>
/// A markdown table being edited in the <see cref="MarkdownEditor"/> table dialog. Row 0 is the header; every row has
/// <see cref="Columns"/> cells. Cells hold the text as users see it: <c>|</c> unescaped and line breaks as
/// <c>&lt;br&gt;</c>.
/// </summary>
internal sealed class MarkdownTable
{
    public List<List<string>> Rows { get; } = [];
    public List<MarkdownTableAlignment> Alignments { get; } = [];

    public int Columns => Alignments.Count;

    /// <summary>A new table with numbered headers and empty body rows.</summary>
    public static MarkdownTable Create(int columns, int bodyRows, string columnText)
    {
        var table = new MarkdownTable();
        table.Alignments.AddRange(Enumerable.Repeat(MarkdownTableAlignment.None, columns));
        table.Rows.Add(Enumerable.Range(1, columns).Select(i => string.Format(columnText, i)).ToList());
        for (var i = 0; i < bodyRows; i++)
        {
            table.Rows.Add(EmptyRow(columns));
        }
        return table;
    }

    /// <summary>From the cells the editor read: header, separator row (alignment markers), then the body rows.</summary>
    public static MarkdownTable FromCells(IReadOnlyList<IReadOnlyList<string>> cells)
    {
        var table = new MarkdownTable();
        var columns = Math.Max(1, cells.Max(row => row.Count));
        var markers = cells.Count > 1 ? cells[1] : [];
        for (var i = 0; i < columns; i++)
        {
            table.Alignments.Add(ParseAlignment(i < markers.Count ? markers[i] : ""));
        }

        for (var r = 0; r < cells.Count; r++)
        {
            if (r == 1)
            {
                continue;
            }
            var row = cells[r].Select(Unescape).ToList();
            row.AddRange(Enumerable.Repeat("", columns - row.Count));
            table.Rows.Add(row);
        }
        return table;
    }

    /// <summary>Header, separator row and body rows as markdown cell text, ready for the editor to line up.</summary>
    public string[][] ToCells()
    {
        var cells = new List<string[]> { Rows[0].Select(Escape).ToArray(), Alignments.Select(Marker).ToArray() };
        cells.AddRange(Rows.Skip(1).Select(row => row.Select(Escape).ToArray()));
        return cells.ToArray();
    }

    public void AddRow(int at)
    {
        Rows.Insert(Math.Clamp(at, 1, Rows.Count), EmptyRow(Columns));
    }

    public void AddColumn(int at)
    {
        at = Math.Clamp(at, 0, Columns);
        Alignments.Insert(at, MarkdownTableAlignment.None);
        foreach (var row in Rows)
        {
            row.Insert(at, "");
        }
    }

    /// <summary>Removes a body row. The header row stays.</summary>
    public void RemoveRow(int row)
    {
        if (row >= 1 && row < Rows.Count)
        {
            Rows.RemoveAt(row);
        }
    }

    /// <summary>Removes a column. The last column stays.</summary>
    public void RemoveColumn(int column)
    {
        if (Columns > 1 && column >= 0 && column < Columns)
        {
            Alignments.RemoveAt(column);
            foreach (var row in Rows)
            {
                row.RemoveAt(column);
            }
        }
    }

    /// <summary>Swaps a body row with the one above or below. The header row does not move.</summary>
    public void MoveRow(int row, int offset)
    {
        var target = row + offset;
        if (row >= 1 && row < Rows.Count && target >= 1 && target < Rows.Count)
        {
            (Rows[row], Rows[target]) = (Rows[target], Rows[row]);
        }
    }

    public void MoveColumn(int column, int offset)
    {
        var target = column + offset;
        if (column >= 0 && column < Columns && target >= 0 && target < Columns)
        {
            (Alignments[column], Alignments[target]) = (Alignments[target], Alignments[column]);
            foreach (var row in Rows)
            {
                (row[column], row[target]) = (row[target], row[column]);
            }
        }
    }

    /// <summary>
    /// Fills cells from text copied from a spreadsheet (tab-separated, quoted cells allowed), starting at the given
    /// cell, and adds the rows and columns it needs.
    /// </summary>
    public void Paste(int row, int column, string text)
    {
        var pasted = ParseDelimited(text);
        if (pasted.Count == 0)
        {
            return;
        }

        var width = pasted.Max(r => r.Count);
        while (Columns < column + width)
        {
            AddColumn(Columns);
        }
        while (Rows.Count < row + pasted.Count)
        {
            AddRow(Rows.Count);
        }

        for (var r = 0; r < pasted.Count; r++)
        {
            for (var c = 0; c < pasted[r].Count; c++)
            {
                Rows[row + r][column + c] = CellText(pasted[r][c]);
            }
        }
    }

    /// <summary>Text cells may not span lines in markdown: line breaks become <c>&lt;br&gt;</c>.</summary>
    public static string CellText(string? text) =>
        (text ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim().Replace("\n", "<br>");

    /// <summary>Splits tab-separated text into rows of cells, as spreadsheets put it on the clipboard.</summary>
    public static List<List<string>> ParseDelimited(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var cell = new StringBuilder();
        var quoted = false;
        var cellStart = true;
        text = text.Replace("\r\n", "\n").Replace('\r', '\n');

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < text.Length && text[i + 1] == '"')
                {
                    cell.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    quoted = false;
                }
                else
                {
                    cell.Append(c);
                }
                continue;
            }

            if (c == '"' && cellStart)
            {
                quoted = true;
                cellStart = false;
            }
            else if (c == '\t')
            {
                row.Add(cell.ToString());
                cell.Clear();
                cellStart = true;
            }
            else if (c == '\n')
            {
                row.Add(cell.ToString());
                rows.Add(row);
                row = [];
                cell.Clear();
                cellStart = true;
            }
            else
            {
                cell.Append(c);
                cellStart = false;
            }
        }

        // A trailing line break does not start another row.
        if (cell.Length > 0 || row.Count > 0)
        {
            row.Add(cell.ToString());
            rows.Add(row);
        }
        return rows;
    }

    /// <summary>A pipe would end the cell: escape it.</summary>
    public static string Escape(string? cell) => CellText(cell).Replace("\\|", "|").Replace("|", "\\|");

    public static string Unescape(string? cell) => (cell ?? "").Replace("\\|", "|");

    private static MarkdownTableAlignment ParseAlignment(string marker)
    {
        marker = marker.Trim();
        var left = marker.StartsWith(':');
        var right = marker.Length > 1 && marker.EndsWith(':');
        return left && right ? MarkdownTableAlignment.Center
            : right ? MarkdownTableAlignment.Right
            : left ? MarkdownTableAlignment.Left
            : MarkdownTableAlignment.None;
    }

    private static string Marker(MarkdownTableAlignment alignment) => alignment switch
    {
        MarkdownTableAlignment.Left => ":--",
        MarkdownTableAlignment.Center => ":-:",
        MarkdownTableAlignment.Right => "--:",
        _ => "---"
    };

    private static List<string> EmptyRow(int columns) => Enumerable.Repeat("", columns).ToList();
}
