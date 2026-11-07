#nullable enable
using System.Globalization;
using System.IO;
using System.Linq.Expressions;
using System.Text;

namespace TabBlazor;

/// <summary>Markdown and file helpers for <see cref="MarkdownEditor"/>.</summary>
internal static class MarkdownSyntax
{
    private static readonly HashSet<string> EmbeddableImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/gif", "image/webp", "image/svg+xml", "image/avif", "image/bmp"
    };

    /// <summary>Escapes text for use inside <c>[…]</c>.</summary>
    public static string EscapeText(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }

        var builder = new StringBuilder(text.Length + 4);
        foreach (var c in text)
        {
            switch (c)
            {
                case '\\' or '[' or ']' or '*' or '_' or '`':
                    builder.Append('\\').Append(c);
                    break;
                case '\r' or '\n' or '\t':
                    builder.Append(' ');
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }
        return builder.ToString();
    }

    /// <summary>Makes a URL safe for use inside <c>(…)</c> without angle brackets.</summary>
    public static string EncodeUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return "";
        }

        var builder = new StringBuilder(url.Length + 8);
        foreach (var c in url)
        {
            switch (c)
            {
                case ' ': builder.Append("%20"); break;
                case '(': builder.Append("%28"); break;
                case ')': builder.Append("%29"); break;
                case '<': builder.Append("%3C"); break;
                case '>': builder.Append("%3E"); break;
                case '\r' or '\n' or '\t': break;
                default: builder.Append(c); break;
            }
        }
        return builder.ToString();
    }

    public static bool IsEmbeddableImage(string? contentType) =>
        contentType is not null && EmbeddableImageTypes.Contains(contentType);

    /// <summary>The markdown inserted for an uploaded file.</summary>
    public static string ForUpload(MarkdownUploadResult result, string fileName, string contentType)
    {
        var embed = result.Embed ?? IsEmbeddableImage(contentType);
        var text = result.Text ?? (embed ? Path.GetFileNameWithoutExtension(fileName) : fileName);
        var markdown = $"[{EscapeText(text)}]({EncodeUrl(result.Url)})";
        return embed ? "!" + markdown : markdown;
    }

    /// <summary>The text that stands in for a file while it uploads. The token makes it unique.</summary>
    public static string Placeholder(string text, string token) => $"![{EscapeText(text)}](#upload-{token})";

    /// <summary>
    /// Whether a file matches an HTML <c>accept</c> list such as <c>"image/*,.pdf"</c>. Null, empty, <c>*</c> and
    /// <c>*/*</c> accept everything.
    /// </summary>
    public static bool Accepts(string? accept, string fileName, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(accept))
        {
            return true;
        }

        contentType ??= "";
        foreach (var raw in accept.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (raw is "*" or "*/*")
            {
                return true;
            }

            if (raw.StartsWith('.'))
            {
                if (fileName.EndsWith(raw, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            else if (raw.EndsWith("/*", StringComparison.Ordinal))
            {
                if (contentType.StartsWith(raw[..^1], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            else if (string.Equals(raw, contentType, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>True when an accept list only lets images through, so the toolbar shows an image button.</summary>
    public static bool AcceptsOnlyImages(string? accept)
    {
        if (string.IsNullOrWhiteSpace(accept))
        {
            return false;
        }

        string[] imageExtensions = [".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg", ".avif", ".bmp"];
        return accept.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .All(t => t.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                      || imageExtensions.Contains(t, StringComparer.OrdinalIgnoreCase));
    }

    public static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        var format = unit == 0 || size >= 10 || size % 1 == 0 ? "0" : "0.#";
        return size.ToString(format, CultureInfo.CurrentCulture) + " " + units[unit];
    }

    /// <summary>
    /// The form field name for a bound expression, such as <c>Model.Notes</c> for <c>() =&gt; Model.Notes</c>,
    /// so the value posts with a static server-rendered form.
    /// </summary>
    public static string? FieldPath(LambdaExpression? expression)
    {
        var parts = new List<string>();
        var current = expression?.Body;
        while (current is MemberExpression member)
        {
            parts.Add(member.Member.Name);
            // Stop at the component or closure that holds the root member (or at a static member).
            if (member.Expression is null or ConstantExpression)
            {
                break;
            }
            current = member.Expression;
        }

        if (current is not MemberExpression { Expression: null or ConstantExpression })
        {
            // Indexers, method calls and the like: let the app set Name itself.
            return null;
        }

        if (parts.Count == 0)
        {
            return null;
        }

        parts.Reverse();
        return string.Join('.', parts);
    }
}
