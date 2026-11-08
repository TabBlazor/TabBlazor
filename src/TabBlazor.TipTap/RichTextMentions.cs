using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TabBlazor;

/// <summary>Reads mentions out of saved <see cref="RichTextEditor"/> content, for example to notify the people mentioned.</summary>
public static partial class RichTextMentions
{
    /// <summary>
    /// The distinct ids of the people mentioned in <paramref name="content"/>, in order of first appearance. The ids come
    /// from the browser: check them against your data before acting on them.
    /// </summary>
    /// <param name="content">The editor's value.</param>
    /// <param name="format">What <paramref name="content"/> holds. Defaults to HTML.</param>
    public static IReadOnlyList<string> GetIds(string? content, RichTextEditorFormat format = RichTextEditorFormat.Html)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        var ids = new List<string>();
        if (format == RichTextEditorFormat.Json)
        {
            try
            {
                using var document = JsonDocument.Parse(content);
                CollectJson(document.RootElement, ids);
            }
            catch (JsonException)
            {
                return [];
            }
        }
        else if (format == RichTextEditorFormat.Markdown)
        {
            foreach (Match shortcode in MarkdownMention().Matches(content))
            {
                if (Attribute(shortcode.Groups["attributes"].Value, "id") is { Length: > 0 } id)
                {
                    ids.Add(id);
                }
            }
        }
        else
        {
            foreach (Match tag in StartTag().Matches(content))
            {
                var attributes = tag.Groups["attributes"].Value;
                if (Attribute(attributes, "data-type") == "mention" && Attribute(attributes, "data-id") is { Length: > 0 } id)
                {
                    ids.Add(id);
                }
            }
        }
        return ids.Distinct(StringComparer.Ordinal).ToList();
    }

    private static void CollectJson(JsonElement node, List<string> ids)
    {
        if (node.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (node.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String && type.GetString() == "mention"
            && node.TryGetProperty("attrs", out var attrs) && attrs.ValueKind == JsonValueKind.Object
            && attrs.TryGetProperty("id", out var id) && id.ValueKind is JsonValueKind.String or JsonValueKind.Number)
        {
            var value = id.ValueKind == JsonValueKind.String ? id.GetString() : id.GetRawText();
            if (!string.IsNullOrEmpty(value))
            {
                ids.Add(value);
            }
        }

        if (node.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in content.EnumerateArray())
            {
                CollectJson(child, ids);
            }
        }
    }

    private static string? Attribute(string attributes, string name)
    {
        foreach (Match attribute in AttributePattern().Matches(attributes))
        {
            if (string.Equals(attribute.Groups["name"].Value, name, StringComparison.OrdinalIgnoreCase))
            {
                var value = attribute.Groups["dq"].Success ? attribute.Groups["dq"].Value
                    : attribute.Groups["sq"].Success ? attribute.Groups["sq"].Value
                    : attribute.Groups["bare"].Value;
                return WebUtility.HtmlDecode(value);
            }
        }
        return null;
    }

    /// <summary>A mention in markdown: <c>[@ id="42" label="Anna"]</c>.</summary>
    [GeneratedRegex(@"\[@(?<attributes>\s[^\]]*)\]", RegexOptions.CultureInvariant)]
    private static partial Regex MarkdownMention();

    [GeneratedRegex(@"<[a-zA-Z][a-zA-Z0-9-]*(?<attributes>(?:\s+[^\s""'>/=]+(?:\s*=\s*(?:""[^""]*""|'[^']*'|[^\s""'=<>`]+))?)*)\s*/?>", RegexOptions.CultureInvariant)]
    private static partial Regex StartTag();

    [GeneratedRegex(@"(?<name>[^\s""'>/=]+)(?:\s*=\s*(?:""(?<dq>[^""]*)""|'(?<sq>[^']*)'|(?<bare>[^\s""'=<>`]+)))?", RegexOptions.CultureInvariant)]
    private static partial Regex AttributePattern();
}
