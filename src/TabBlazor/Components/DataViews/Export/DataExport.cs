#nullable enable
using System.Globalization;
using System.IO;
using System.Text;

namespace TabBlazor;

/// <summary>
/// Writes a <c>DataView</c> export as CSV. This is a convenience, not a format registry: an app that wants
/// another format writes it in its own <c>OnExport</c> callback with its own library.
/// </summary>
/// <remarks>
/// It handles the four things a hand-written CSV usually gets wrong: the delimiter of the current culture
/// (<c>;</c> where the decimal separator is a comma), a UTF-8 byte order mark so Excel reads non-ASCII text,
/// RFC 4180 quoting, and a prefix on cells that would otherwise be read as a formula.
/// </remarks>
public static class DataExport
{
    /// <summary>The content type to download a CSV file with.</summary>
    public const string CsvContentType = "text/csv";

    /// <summary>Writes the export as CSV and returns the bytes, ready for <c>TablerService.SaveAsBinary</c>.</summary>
    public static async Task<byte[]> ToCsvAsync<TItem>(
        DataExportContext<TItem> context,
        CsvExportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream();
        await WriteCsvAsync(stream, context, options, cancellationToken);
        return stream.ToArray();
    }

    /// <summary>Writes the export as CSV to a stream, a page of rows at a time.</summary>
    public static async Task WriteCsvAsync<TItem>(
        Stream stream,
        DataExportContext<TItem> context,
        CsvExportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(context);

        var settings = options ?? new CsvExportOptions();
        var culture = settings.Culture ?? CultureInfo.CurrentCulture;
        var delimiter = settings.Delimiter ?? culture.TextInfo.ListSeparator;
        var encoding = new UTF8Encoding(settings.ByteOrderMark);

        await using var writer = new StreamWriter(stream, encoding, leaveOpen: true) { NewLine = "\r\n" };

        if (settings.IncludeHeader)
        {
            await WriteRowAsync(writer, context.Columns.Select(c => c.Title), delimiter, settings, culture);
        }

        await foreach (var item in context.GetItemsAsync(cancellationToken).WithCancellation(cancellationToken))
        {
            await WriteRowAsync(writer, context.Columns.Select(c => c.GetExportText(item)), delimiter, settings, culture);
        }

        await writer.FlushAsync(cancellationToken);
    }

    private static async Task WriteRowAsync(
        TextWriter writer,
        IEnumerable<string?> cells,
        string delimiter,
        CsvExportOptions options,
        CultureInfo culture)
    {
        var first = true;
        foreach (var cell in cells)
        {
            if (!first)
            {
                await writer.WriteAsync(delimiter);
            }

            first = false;
            await writer.WriteAsync(Escape(cell, delimiter, options, culture));
        }

        await writer.WriteLineAsync();
    }

    /// <summary>Quotes a cell when it needs it (RFC 4180) and defuses text a spreadsheet would run as a formula.</summary>
    internal static string Escape(string? value, string delimiter, CsvExportOptions options, CultureInfo culture)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var text = options.ProtectAgainstFormulas && IsFormula(value, culture) ? "'" + value : value;

        var mustQuote = text.Contains('"') || text.Contains('\n') || text.Contains('\r') ||
                        text.Contains(delimiter, StringComparison.Ordinal) ||
                        char.IsWhiteSpace(text[0]) || char.IsWhiteSpace(text[^1]);

        return mustQuote ? '"' + text.Replace("\"", "\"\"") + '"' : text;
    }

    /// <summary>
    /// True when a spreadsheet would read the text as a formula. A leading minus is left alone when the cell is a
    /// number, so negative amounts stay numbers.
    /// </summary>
    private static bool IsFormula(string value, CultureInfo culture) => value[0] switch
    {
        '=' or '+' or '@' or '\t' or '\r' => true,
        '-' => !double.TryParse(value, NumberStyles.Any, culture, out _),
        _ => false
    };
}

/// <summary>How <see cref="DataExport"/> writes a CSV file.</summary>
public sealed record CsvExportOptions
{
    /// <summary>The column separator. Null (the default) takes the list separator of <see cref="Culture"/>.</summary>
    public string? Delimiter { get; init; }

    /// <summary>The culture that formats values and gives the delimiter. Null takes the current culture.</summary>
    public CultureInfo? Culture { get; init; }

    /// <summary>Writes a UTF-8 byte order mark, so Excel reads non-ASCII text. Defaults to true.</summary>
    public bool ByteOrderMark { get; init; } = true;

    /// <summary>Writes the column titles as the first row. Defaults to true.</summary>
    public bool IncludeHeader { get; init; } = true;

    /// <summary>
    /// Prefixes cells starting with <c>=</c>, <c>+</c>, <c>@</c> or a non-numeric <c>-</c> with an apostrophe, so
    /// a spreadsheet does not run them. Defaults to true.
    /// </summary>
    public bool ProtectAgainstFormulas { get; init; } = true;
}
