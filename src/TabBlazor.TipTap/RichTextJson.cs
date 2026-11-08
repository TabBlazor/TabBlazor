using System.Globalization;
using System.Text;
using System.Text.Json;

namespace TabBlazor;

/// <summary>
/// Turns the TipTap JSON document from a <see cref="RichTextEditor"/> with <see cref="RichTextEditorFormat.Json"/> into
/// HTML, in .NET, without a browser: for <see cref="RichTextView"/>, emails, PDFs or API responses.
/// </summary>
/// <remarks>
/// The HTML is safe by construction, so it needs no sanitizer: text and attribute values are encoded, only the
/// attributes listed here are written, links keep only http, https, mailto and relative URLs, and images only http,
/// https and relative ones. Nodes of unknown types are left out, with their content, unless
/// <see cref="RichTextJsonOptions.Nodes"/> has a renderer for them; marks of unknown types are left out and their text kept.
/// </remarks>
public static class RichTextJson
{
    private const string MentionClass = "rich-text-mention";
    private const string LinkRel = "noopener noreferrer nofollow";

    /// <summary>The HTML for <paramref name="json"/>, the editor's value; empty when it is null or empty.</summary>
    /// <param name="json">A TipTap document: <c>{"type":"doc","content":[...]}</c>.</param>
    /// <param name="options">Renderers for custom node types, or null.</param>
    /// <exception cref="JsonException"><paramref name="json"/> is not valid JSON.</exception>
    public static string ToHtml(string? json, RichTextJsonOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return "";
        }

        using var document = JsonDocument.Parse(json);
        return ToHtml(document.RootElement, options);
    }

    /// <summary>The HTML for a parsed TipTap document or node.</summary>
    public static string ToHtml(JsonElement node, RichTextJsonOptions? options = null)
    {
        var html = new StringBuilder();
        new Writer(html, options).Node(node);
        return html.ToString();
    }

    /// <summary>
    /// Encodes <paramref name="text"/> for HTML text or a quoted attribute value. Use it in a
    /// <see cref="RichTextJsonOptions.Nodes"/> renderer for every value that comes from the document.
    /// </summary>
    public static string Encode(string? text)
    {
        if (string.IsNullOrEmpty(text) || text.AsSpan().IndexOfAny("&<>\"") < 0)
        {
            return text ?? "";
        }

        // Only what HTML needs: non-ASCII letters stay readable, unlike WebUtility.HtmlEncode.
        var encoded = new StringBuilder(text.Length + 16);
        foreach (var c in text)
        {
            encoded.Append(c switch
            {
                '&' => "&amp;",
                '<' => "&lt;",
                '>' => "&gt;",
                '"' => "&quot;",
                _ => c.ToString()
            });
        }
        return encoded.ToString();
    }

    /// <summary>
    /// Whether <paramref name="url"/> may be a link: http, https, mailto or relative. Use it in a
    /// <see cref="RichTextJsonOptions.Nodes"/> renderer before writing a URL from the document.
    /// </summary>
    public static bool IsAllowedUrl(string? url) => IsAllowedUrl(url, allowMailto: true);

    private static bool IsAllowedUrl(string? url, bool allowMailto)
    {
        // Browsers drop whitespace and control characters inside a scheme ("java\tscript:"), so check without them.
        var value = new string((url ?? "").Where(c => !char.IsWhiteSpace(c) && !char.IsControl(c)).ToArray());
        if (value.Length == 0)
        {
            return false;
        }

        var colon = value.IndexOf(':');
        var hasScheme = colon > 0 && char.IsAsciiLetter(value[0])
            && value[..colon].All(c => char.IsAsciiLetterOrDigit(c) || c is '+' or '.' or '-');
        if (!hasScheme)
        {
            return true;
        }

        var scheme = value[..colon];
        return scheme.Equals("http", StringComparison.OrdinalIgnoreCase)
            || scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
            || (allowMailto && scheme.Equals("mailto", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class Writer(StringBuilder html, RichTextJsonOptions? options)
    {
        // The marks open around the current text, outermost first. Text nodes that share leading marks share the elements,
        // as in TipTap: "<strong>a <em>b</em></strong>", not "<strong>a </strong><strong><em>b</em></strong>".
        private readonly List<(JsonElement Mark, string Close)> openMarks = [];

        public void Node(JsonElement node)
        {
            if (node.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            var type = String(node, "type");
            if (type != "text")
            {
                CloseMarks(0);
            }

            switch (type)
            {
                case "doc":
                    Children(node);
                    break;
                case "text":
                    Text(node);
                    break;
                case "paragraph":
                    Block(node, "p", AlignStyle(node));
                    break;
                case "heading":
                    var level = Math.Clamp(Int(Attrs(node), "level") ?? 1, 1, 6);
                    Block(node, "h" + level.ToString(CultureInfo.InvariantCulture), AlignStyle(node));
                    break;
                case "blockquote":
                    Block(node, "blockquote");
                    break;
                case "bulletList":
                    Block(node, "ul");
                    break;
                case "orderedList":
                    OrderedList(node);
                    break;
                case "listItem":
                    Block(node, "li");
                    break;
                case "taskList":
                    Block(node, "ul", ("data-type", "taskList"));
                    break;
                case "taskItem":
                    TaskItem(node);
                    break;
                case "codeBlock":
                    CodeBlock(node);
                    break;
                case "horizontalRule":
                    html.Append("<hr>");
                    break;
                case "hardBreak":
                    html.Append("<br>");
                    break;
                case "image":
                    Image(node);
                    break;
                case "mention":
                    Mention(node);
                    break;
                case "table":
                    html.Append("<table><tbody>");
                    Children(node);
                    html.Append("</tbody></table>");
                    break;
                case "tableRow":
                    Block(node, "tr");
                    break;
                case "tableHeader":
                    Cell(node, "th");
                    break;
                case "tableCell":
                    Cell(node, "td");
                    break;
                default:
                    Custom(node, type);
                    break;
            }
        }

        private void Children(JsonElement node)
        {
            if (node.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in content.EnumerateArray())
                {
                    Node(child);
                }
            }
            CloseMarks(0);
        }

        private void Block(JsonElement node, string tag, params (string Name, string? Value)[] attributes)
        {
            Open(tag, attributes);
            Children(node);
            html.Append("</").Append(tag).Append('>');
        }

        private void Open(string tag, params (string Name, string? Value)[] attributes)
        {
            html.Append('<').Append(tag);
            foreach (var (name, value) in attributes)
            {
                if (value is not null)
                {
                    html.Append(' ').Append(name).Append("=\"").Append(Encode(value)).Append('"');
                }
            }
            html.Append('>');
        }

        private void Text(JsonElement node)
        {
            var text = String(node, "text");
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            var marks = node.TryGetProperty("marks", out var m) && m.ValueKind == JsonValueKind.Array
                ? m.EnumerateArray().Where(mark => MarkTag(mark) is not null).ToList()
                : [];

            var keep = 0;
            while (keep < openMarks.Count && keep < marks.Count && SameMark(openMarks[keep].Mark, marks[keep]))
            {
                keep++;
            }
            CloseMarks(keep);

            for (var i = keep; i < marks.Count; i++)
            {
                var (open, close) = MarkTag(marks[i])!.Value;
                html.Append(open);
                openMarks.Add((marks[i], close));
            }

            html.Append(Encode(text));
        }

        private void CloseMarks(int keep)
        {
            for (var i = openMarks.Count - 1; i >= keep; i--)
            {
                html.Append(openMarks[i].Close);
                openMarks.RemoveAt(i);
            }
        }

        private static bool SameMark(JsonElement a, JsonElement b) =>
            String(a, "type") == String(b, "type") && RawAttrs(a) == RawAttrs(b);

        private static string RawAttrs(JsonElement mark) =>
            Attrs(mark) is { ValueKind: JsonValueKind.Object } attrs ? attrs.GetRawText() : "";

        private static (string Open, string Close)? MarkTag(JsonElement mark) => String(mark, "type") switch
        {
            "bold" => ("<strong>", "</strong>"),
            "italic" => ("<em>", "</em>"),
            "underline" => ("<u>", "</u>"),
            "strike" => ("<s>", "</s>"),
            "code" => ("<code>", "</code>"),
            "highlight" => ("<mark>", "</mark>"),
            "link" => LinkTag(Attrs(mark)),
            _ => null
        };

        // A link the editor would not allow keeps its text, without the link.
        private static (string, string)? LinkTag(JsonElement attrs)
        {
            var href = String(attrs, "href");
            if (!IsAllowedUrl(href, allowMailto: true))
            {
                return ("", "");
            }

            var title = String(attrs, "title");
            var open = $"<a rel=\"{LinkRel}\" href=\"{Encode(href)}\"" + (title is null ? "" : $" title=\"{Encode(title)}\"") + ">";
            return (open, "</a>");
        }

        private void OrderedList(JsonElement node)
        {
            var attrs = Attrs(node);
            var start = Int(attrs, "start");
            var type = String(attrs, "type");
            Block(node, "ol",
                ("start", start is { } s && s != 1 ? s.ToString(CultureInfo.InvariantCulture) : null),
                ("type", type is "1" or "a" or "A" or "i" or "I" ? type : null));
        }

        private void TaskItem(JsonElement node)
        {
            var isChecked = Attrs(node).TryGetProperty("checked", out var c) && c.ValueKind == JsonValueKind.True;
            Open("li", ("data-checked", isChecked ? "true" : "false"), ("data-type", "taskItem"));
            html.Append(isChecked ? "<label><input type=\"checkbox\" checked=\"checked\"><span></span></label><div>"
                : "<label><input type=\"checkbox\"><span></span></label><div>");
            Children(node);
            html.Append("</div></li>");
        }

        private void CodeBlock(JsonElement node)
        {
            var language = String(Attrs(node), "language");
            html.Append("<pre>");
            Open("code", ("class", string.IsNullOrWhiteSpace(language) ? null : "language-" + language));
            // Code blocks hold plain text: marks do not apply.
            if (node.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in content.EnumerateArray())
                {
                    html.Append(Encode(String(child, "text")));
                }
            }
            html.Append("</code></pre>");
        }

        private void Image(JsonElement node)
        {
            var attrs = Attrs(node);
            var src = String(attrs, "src");
            if (!IsAllowedUrl(src, allowMailto: false))
            {
                return;
            }

            Open("img", ("src", src), ("alt", String(attrs, "alt")), ("title", String(attrs, "title")),
                ("width", Int(attrs, "width")?.ToString(CultureInfo.InvariantCulture)),
                ("height", Int(attrs, "height")?.ToString(CultureInfo.InvariantCulture)));
        }

        private void Mention(JsonElement node)
        {
            var attrs = Attrs(node);
            var id = String(attrs, "id");
            var label = String(attrs, "label");
            var trigger = String(attrs, "mentionSuggestionChar") ?? "@";
            Open("span", ("class", MentionClass), ("data-type", "mention"), ("data-id", id), ("data-label", label),
                ("data-mention-suggestion-char", trigger));
            html.Append(Encode(trigger + (label ?? id)));
            html.Append("</span>");
        }

        private void Cell(JsonElement node, string tag)
        {
            var attrs = Attrs(node);
            var colspan = Int(attrs, "colspan");
            var rowspan = Int(attrs, "rowspan");
            var align = String(attrs, "align");
            Block(node, tag,
                ("colspan", colspan is > 1 ? colspan.Value.ToString(CultureInfo.InvariantCulture) : null),
                ("rowspan", rowspan is > 1 ? rowspan.Value.ToString(CultureInfo.InvariantCulture) : null),
                ("style", IsAlignment(align) ? $"text-align: {align}" : null));
        }

        private void Custom(JsonElement node, string? type)
        {
            if (type is null || options is null || !options.Nodes.TryGetValue(type, out var render))
            {
                return;
            }

            var inner = new StringBuilder();
            new Writer(inner, options).Children(node);
            html.Append(render(new RichTextJsonNode(type, node, inner.ToString())));
        }

        private static (string, string?) AlignStyle(JsonElement node)
        {
            var align = String(Attrs(node), "textAlign");
            return ("style", IsAlignment(align) ? $"text-align: {align};" : null);
        }

        private static bool IsAlignment(string? value) => value is "left" or "center" or "right" or "justify";
    }

    internal static JsonElement Attrs(JsonElement node) =>
        node.ValueKind == JsonValueKind.Object && node.TryGetProperty("attrs", out var attrs) && attrs.ValueKind == JsonValueKind.Object
            ? attrs
            : default;

    internal static string? String(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
        {
            return null;
        }
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
    }

    internal static int? Int(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(name, out var value))
        {
            return null;
        }
        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out var number) => number,
            _ => null
        };
    }
}

/// <summary>Settings for <see cref="RichTextJson.ToHtml(string?, RichTextJsonOptions?)"/>.</summary>
public sealed class RichTextJsonOptions
{
    /// <summary>
    /// Renderers for node types of your own TipTap extensions, by type name. A renderer returns the node's HTML and must
    /// encode every value from the document with <see cref="RichTextJson.Encode"/>:
    /// <code>options.Nodes["callout"] = node =&gt; $"&lt;div class=\"callout\"&gt;{node.ContentHtml}&lt;/div&gt;";</code>
    /// </summary>
    public IDictionary<string, Func<RichTextJsonNode, string>> Nodes { get; } = new Dictionary<string, Func<RichTextJsonNode, string>>();
}

/// <summary>A node of a custom type, for a <see cref="RichTextJsonOptions.Nodes"/> renderer.</summary>
/// <param name="Type">The node's type name.</param>
/// <param name="Json">The node as stored, with its <c>attrs</c>. Values in it come from the browser.</param>
/// <param name="ContentHtml">The HTML of the node's content, already rendered and safe.</param>
public sealed record RichTextJsonNode(string Type, JsonElement Json, string ContentHtml)
{
    /// <summary>The attribute <paramref name="name"/> as text, or null. Encode it before writing it into HTML.</summary>
    public string? Attr(string name) => RichTextJson.String(RichTextJson.Attrs(Json), name);
}
