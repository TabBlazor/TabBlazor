#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabBlazor.Tests.Components.DataViews
{
    public class DataExportCsvTests : TabBlazorTestContext
    {
        private static readonly CultureInfo Swedish = CultureInfo.GetCultureInfo("sv-SE");
        private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

        private async Task<string> CsvAsync(
            System.Collections.Generic.List<Row> rows,
            CsvExportOptions? options = null)
        {
            string? csv = null;
            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Items, rows)
                .Add(x => x.OnExport, async context =>
                {
                    var bytes = await DataExport.ToCsvAsync(context, options);
                    // The byte order mark has its own test; here it would only get in the way.
                    csv = Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF');
                })
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name))
                .AddChildContent<DataColumn<Row, decimal>>(c => c.Add(x => x.Value, r => r.Total).Add(x => x.Format, "N2")));

            await cut.InvokeAsync(() => cut.Instance.ExportAsync(DataExportScope.CurrentPage));
            return csv!;
        }

        [Fact]
        public async Task Writes_a_header_row_and_one_line_per_row()
        {
            var csv = await CsvAsync(Row.Many(2), new CsvExportOptions { Culture = English });

            var lines = csv.TrimEnd('\r', '\n').Split("\r\n");
            Assert.Equal(3, lines.Length);
            Assert.Equal("Name,Total", lines[0]);
            Assert.StartsWith("Row 01,", lines[1]);
        }

        [Fact]
        public async Task Leaves_out_the_header_when_asked()
        {
            var csv = await CsvAsync(Row.Many(1), new CsvExportOptions { Culture = English, IncludeHeader = false });

            Assert.StartsWith("Row 01,", csv);
        }

        [Fact]
        public async Task Takes_the_delimiter_from_the_culture()
        {
            var swedish = await CsvAsync(Row.Many(1), new CsvExportOptions { Culture = Swedish });
            var english = await CsvAsync(Row.Many(1), new CsvExportOptions { Culture = English });

            Assert.Contains("Name;Total", swedish);
            Assert.Contains("Name,Total", english);
        }

        [Fact]
        public async Task Writes_a_byte_order_mark_so_Excel_reads_non_ascii_text()
        {
            byte[]? withBom = null;
            byte[]? withoutBom = null;
            var rows = new System.Collections.Generic.List<Row> { new() { Id = 1, Name = "Öresund" } };

            var cut = Render<DataView<Row>>(p => p
                .Add(x => x.Items, rows)
                .Add(x => x.OnExport, async context =>
                {
                    withBom = await DataExport.ToCsvAsync(context);
                    withoutBom = await DataExport.ToCsvAsync(context, new CsvExportOptions { ByteOrderMark = false });
                })
                .AddChildContent<DataColumn<Row, string>>(c => c.Add(x => x.Value, r => r.Name)));

            await cut.InvokeAsync(() => cut.Instance.ExportAsync(DataExportScope.CurrentPage));

            Assert.Equal<byte[]>([0xEF, 0xBB, 0xBF], withBom!.Take(3).ToArray());
            Assert.NotEqual<byte>(0xEF, withoutBom![0]);
            Assert.Contains("Öresund", Encoding.UTF8.GetString(withBom!));
        }

        [Fact]
        public async Task Formats_values_the_way_the_cell_shows_them()
        {
            var rows = new System.Collections.Generic.List<Row> { new() { Id = 1, Name = "A", Total = 1234.5m } };

            var swedish = await CsvAsync(rows, new CsvExportOptions { Culture = Swedish });

            // The cell text follows the UI culture; the delimiter keeps it in its own field either way.
            Assert.Contains(1234.5m.ToString("N2"), swedish);
        }

        // The quoting and formula rules are worth testing on their own: they are what a hand-written CSV gets wrong.

        [Theory]
        [InlineData("plain", "plain")]
        [InlineData("has,comma", "\"has,comma\"")]
        [InlineData("has\"quote", "\"has\"\"quote\"")]
        [InlineData("two\nlines", "\"two\nlines\"")]
        [InlineData(" padded ", "\" padded \"")]
        [InlineData("", "")]
        public void Quotes_a_cell_only_when_it_needs_it(string value, string expected) =>
            Assert.Equal(expected, DataExport.Escape(value, ",", new CsvExportOptions(), English));

        [Theory]
        [InlineData("=SUM(A1:A2)", "'=SUM(A1:A2)")]
        [InlineData("+1234", "'+1234")]
        [InlineData("@cmd", "'@cmd")]
        [InlineData("-HYPERLINK(1)", "'-HYPERLINK(1)")]
        public void Defuses_text_a_spreadsheet_would_run_as_a_formula(string value, string expected) =>
            Assert.Equal(expected, DataExport.Escape(value, ",", new CsvExportOptions(), English));

        [Theory]
        [InlineData("-42")]
        [InlineData("-1.5")]
        public void Leaves_negative_numbers_as_numbers(string value) =>
            Assert.Equal(value, DataExport.Escape(value, ",", new CsvExportOptions(), English));

        [Fact]
        public void Keeps_formulas_as_they_are_when_protection_is_off() =>
            Assert.Equal("=SUM(A1:A2)",
                DataExport.Escape("=SUM(A1:A2)", ",", new CsvExportOptions { ProtectAgainstFormulas = false }, English));

        [Fact]
        public void Quotes_a_cell_holding_the_semicolon_delimiter() =>
            Assert.Equal("\"a;b\"", DataExport.Escape("a;b", ";", new CsvExportOptions(), Swedish));
    }
}
