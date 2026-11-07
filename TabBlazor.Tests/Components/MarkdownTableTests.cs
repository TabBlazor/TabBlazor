using System.Collections.Generic;
using System.Linq;

namespace TabBlazor.Tests.Components
{
    public class MarkdownTableTests
    {
        private static MarkdownTable Sample() => MarkdownTable.FromCells(
        [
            ["Name", "Qty"],
            [":--", "--:"],
            ["Apples", "3"],
            ["a \\| b", "`x \\| y`"]
        ]);

        [Fact]
        public void Reads_cells_alignments_and_unescapes_pipes()
        {
            var table = Sample();

            Assert.Equal(2, table.Columns);
            Assert.Equal([MarkdownTableAlignment.Left, MarkdownTableAlignment.Right], table.Alignments);
            Assert.Equal(["Name", "Qty"], table.Rows[0]);
            Assert.Equal(["a | b", "`x | y`"], table.Rows[2]);
        }

        [Fact]
        public void Writes_cells_with_markers_and_escaped_pipes()
        {
            var table = Sample();
            table.Alignments[0] = MarkdownTableAlignment.Center;
            table.Alignments[1] = MarkdownTableAlignment.None;

            var cells = table.ToCells();

            Assert.Equal(["Name", "Qty"], cells[0]);
            Assert.Equal([":-:", "---"], cells[1]);
            Assert.Equal(["a \\| b", "`x \\| y`"], cells[3]);
        }

        [Fact]
        public void Pads_short_rows_and_reads_missing_markers_as_unaligned()
        {
            var table = MarkdownTable.FromCells([["A", "B", "C"], ["---"], ["1"]]);

            Assert.Equal(3, table.Columns);
            Assert.All(table.Alignments, a => Assert.Equal(MarkdownTableAlignment.None, a));
            Assert.Equal(["1", "", ""], table.Rows[1]);
        }

        [Fact]
        public void Creates_numbered_headers_and_empty_rows()
        {
            var table = MarkdownTable.Create(2, 2, "Kolumn {0}");

            Assert.Equal(["Kolumn 1", "Kolumn 2"], table.Rows[0]);
            Assert.Equal(3, table.Rows.Count);
            Assert.All(table.Rows.Skip(1), row => Assert.Equal(["", ""], row));
        }

        [Fact]
        public void Adds_removes_and_moves_rows_but_keeps_the_header()
        {
            var table = Sample();

            table.AddRow(1);
            Assert.Equal(["", ""], table.Rows[1]);
            table.RemoveRow(1);
            table.MoveRow(2, -1);
            Assert.Equal("a | b", table.Rows[1][0]);
            table.MoveRow(1, -1);
            Assert.Equal("Name", table.Rows[0][0]);
            table.RemoveRow(0);
            Assert.Equal("Name", table.Rows[0][0]);
        }

        [Fact]
        public void Adds_removes_and_moves_columns_with_their_alignment_but_keeps_one()
        {
            var table = Sample();

            table.MoveColumn(0, 1);
            Assert.Equal(["Qty", "Name"], table.Rows[0]);
            Assert.Equal(MarkdownTableAlignment.Right, table.Alignments[0]);
            table.AddColumn(1);
            Assert.Equal(["Qty", "", "Name"], table.Rows[0]);
            table.RemoveColumn(0);
            table.RemoveColumn(0);
            table.RemoveColumn(0);
            Assert.Equal(["Name"], table.Rows[0]);
        }

        [Fact]
        public void Parses_spreadsheet_text_with_quoted_cells()
        {
            var rows = MarkdownTable.ParseDelimited("a\tb\r\n\"multi\r\nline\"\t\"say \"\"hi\"\"\"\r\n");

            Assert.Equal(2, rows.Count);
            Assert.Equal(["a", "b"], rows[0]);
            Assert.Equal(["multi\nline", "say \"hi\""], rows[1]);
        }

        [Fact]
        public void Paste_fills_from_the_cell_and_grows_the_table()
        {
            var table = Sample();

            table.Paste(2, 1, "1\tx\n2\ty\n3\tz");

            Assert.Equal(3, table.Columns);
            Assert.Equal(5, table.Rows.Count);
            Assert.Equal(["Apples", "3", ""], table.Rows[1]);
            Assert.Equal(["a | b", "1", "x"], table.Rows[2]);
            Assert.Equal(["", "3", "z"], table.Rows[4]);
            Assert.Equal("", table.Rows[0][2]);
        }

        [Fact]
        public void Line_breaks_in_cells_become_br()
        {
            var table = MarkdownTable.Create(1, 1, "H");

            table.Paste(1, 0, "\"one\ntwo\"\n");

            Assert.Equal("one<br>two", table.Rows[1][0]);
        }
    }
}
