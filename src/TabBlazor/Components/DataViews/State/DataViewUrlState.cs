#nullable enable
using System.Globalization;
using System.Text.RegularExpressions;

namespace TabBlazor;

/// <summary>Whether URL changes replace the current history entry or add one.</summary>
public enum UrlHistoryMode
{
    /// <summary>Replace the history entry (default).</summary>
    Replace,
    /// <summary>Add an entry for page, sort and filter changes, so the back button steps through them.</summary>
    Push
}

/// <summary>A DataView's state as read from a URL, before it is checked against the columns.</summary>
public sealed record DataViewUrlValues(DataViewState State, string? View, string? Current);

/// <summary>
/// Reads and writes a DataView's state in the query string:
/// <c>orders.q=odi&amp;orders.sort=-orderDate&amp;orders.page=3&amp;orders.size=50&amp;orders.f.status=in:Open,Closed</c>.
/// Values use the invariant culture and ISO 8601 dates. Only values that differ from the defaults are written.
/// </summary>
public static partial class DataViewUrlState
{
    [GeneratedRegex("^[A-Za-z0-9_-]+$")]
    private static partial Regex UrlSafe();

    /// <summary>The key prefix for a view id: <c>orders.</c>, or empty without an id. Throws when the id is not URL-safe.</summary>
    public static string Prefix(string? id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return string.Empty;
        }

        if (!UrlSafe().IsMatch(id))
        {
            throw new InvalidOperationException(
                $"DataView Id '{id}' is used as the URL prefix with UrlState and must contain only letters, digits, '-' and '_'.");
        }

        return id + ".";
    }

    /// <summary>
    /// The query parameters for a state. Keys whose value equals the default are set to null, so
    /// <c>NavigationManager.GetUriWithQueryParameters</c> removes them and other parameters on the page are kept.
    /// </summary>
    public static Dictionary<string, object?> ToQuery(string prefix, DataViewState state, DataViewState defaults,
        IEnumerable<string> filterColumns, string? view = null, string? current = null)
    {
        var query = new Dictionary<string, object?>
        {
            [prefix + "q"] = string.IsNullOrWhiteSpace(state.Search) ? null : state.Search,
            [prefix + "sort"] = state.Sorts.SequenceEqual(defaults.Sorts) ? null : FormatSorts(state.Sorts),
            [prefix + "page"] = state.Page <= 1 ? null : state.Page.ToString(CultureInfo.InvariantCulture),
            [prefix + "size"] = state.PageSize == defaults.PageSize ? null : state.PageSize.ToString(CultureInfo.InvariantCulture),
            [prefix + "view"] = view,
            [prefix + "current"] = current
        };

        foreach (var column in filterColumns)
        {
            query[prefix + "f." + column] = null;
        }

        foreach (var filter in state.Filters)
        {
            query[prefix + "f." + filter.Column] = FormatFilter(filter);
        }

        return query;
    }

    // An explicit empty sort ("none") is written when the default sort was cleared.
    private static string FormatSorts(IReadOnlyList<SortSpec> sorts) =>
        sorts.Count == 0 ? "none" : string.Join(",", sorts.Select(s => (s.Descending ? "-" : string.Empty) + s.Column));

    public static string FormatFilter(FilterSpec filter)
    {
        var token = DataFilters.Token(filter.Operator);
        return filter.Operator switch
        {
            FilterOperator.IsNull or FilterOperator.IsNotNull => token,
            FilterOperator.In => $"{token}:{string.Join(",", DataFilters.AsList(filter.Value).Select(DataFilters.FormatValue))}",
            FilterOperator.Between => $"{token}:{DataFilters.FormatValue(filter.Value)},{DataFilters.FormatValue(filter.Value2)}",
            _ => $"{token}:{DataFilters.FormatValue(filter.Value)}"
        };
    }

    /// <summary>Reads the state from a URL. Values are not checked against the columns yet (the view does that).</summary>
    public static DataViewUrlValues? FromUri(string prefix, string uri, DataViewState defaults)
    {
        var query = ParseQuery(uri);
        if (!query.Keys.Any(k => k.StartsWith(prefix, StringComparison.Ordinal) && IsOwnKey(k[prefix.Length..])))
        {
            return null;
        }

        string? Get(string key) => query.GetValueOrDefault(prefix + key);

        var page = int.TryParse(Get("page"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var p) && p > 0 ? p : 1;
        var size = int.TryParse(Get("size"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var s) && s is > 0 and <= 1000
            ? s
            : defaults.PageSize;

        IReadOnlyList<SortSpec> sorts = defaults.Sorts;
        if (Get("sort") is { Length: > 0 } sortText)
        {
            sorts = sortText == "none"
                ? []
                : sortText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(t => t.StartsWith('-') ? new SortSpec(t[1..], true) : new SortSpec(t))
                    .ToList();
        }

        var filters = new List<FilterSpec>();
        var filterPrefix = prefix + "f.";
        foreach (var (key, value) in query.Where(q => q.Key.StartsWith(filterPrefix, StringComparison.Ordinal)))
        {
            if (ParseFilter(key[filterPrefix.Length..], value) is { } filter)
            {
                filters.Add(filter);
            }
        }

        var state = new DataViewState
        {
            Page = page,
            PageSize = size,
            Sorts = sorts,
            Search = Get("q"),
            Filters = filters
        };
        return new DataViewUrlValues(state, Get("view"), Get("current"));
    }

    private static bool IsOwnKey(string key) =>
        key is "q" or "sort" or "page" or "size" or "view" or "current" || key.StartsWith("f.", StringComparison.Ordinal);

    public static FilterSpec? ParseFilter(string column, string? text)
    {
        if (string.IsNullOrWhiteSpace(column) || string.IsNullOrEmpty(text))
        {
            return null;
        }

        var colon = text.IndexOf(':');
        var token = colon < 0 ? text : text[..colon];
        var rest = colon < 0 ? string.Empty : text[(colon + 1)..];
        if (DataFilters.ParseToken(token) is not { } op)
        {
            return null;
        }

        switch (op)
        {
            case FilterOperator.IsNull or FilterOperator.IsNotNull:
                return new FilterSpec(column, op);
            case FilterOperator.In:
                var values = rest.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return values.Length == 0 ? null : new FilterSpec(column, op, values.ToList());
            case FilterOperator.Between:
                var parts = rest.Split(',', 2);
                var from = parts[0].Length == 0 ? null : parts[0];
                var to = parts.Length < 2 || parts[1].Length == 0 ? null : parts[1];
                return from == null && to == null ? null : new FilterSpec(column, op, from, to);
            default:
                return rest.Length == 0 ? null : new FilterSpec(column, op, rest);
        }
    }

    /// <summary>Parses a URL's query string ('+' means a space).</summary>
    public static Dictionary<string, string> ParseQuery(string uri)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var start = uri.IndexOf('?');
        if (start < 0)
        {
            return result;
        }

        var end = uri.IndexOf('#', start);
        var query = end < 0 ? uri[(start + 1)..] : uri[(start + 1)..end];
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var equals = pair.IndexOf('=');
            var key = Uri.UnescapeDataString((equals < 0 ? pair : pair[..equals]).Replace('+', ' '));
            var value = equals < 0 ? string.Empty : Uri.UnescapeDataString(pair[(equals + 1)..].Replace('+', ' '));
            result[key] = value;
        }

        return result;
    }
}

/// <summary>Keeps track of the URL prefixes in use, so two views with <c>UrlState</c> cannot share one.</summary>
public sealed class DataViewUrlRegistry
{
    private readonly HashSet<string> prefixes = [];

    public void Register(string prefix)
    {
        if (!prefixes.Add(prefix))
        {
            throw new InvalidOperationException(prefix.Length == 0
                ? "Two DataViews with UrlState on one page need distinct Ids."
                : $"Two DataViews with UrlState use the Id '{prefix.TrimEnd('.')}'. Give them distinct Ids.");
        }
    }

    public void Unregister(string prefix) => prefixes.Remove(prefix);
}
