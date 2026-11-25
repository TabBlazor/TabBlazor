using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TabBlazor.Linq2GraphQL;

/// <summary>
/// HotChocolate rejected a query for its cost (<c>HC0047</c>). HotChocolate costs the <c>where</c> and <c>order</c>
/// variables the adapter sends as the worst case, so the default limit rejects most filtered queries. Turn the limit
/// off (<c>.ModifyCostOptions(o =&gt; o.EnforceCostLimits = false)</c>) or raise <c>MaxFieldCost</c> on the server.
/// </summary>
public sealed partial class GraphQLCostLimitException : Exception
{
    /// <summary>HotChocolate's error code for an exceeded field cost.</summary>
    public const string ErrorCode = "HC0047";

    public GraphQLCostLimitException(double? fieldCost, double? maxFieldCost, Exception innerException)
        : base(CreateMessage(fieldCost, maxFieldCost), innerException)
    {
        FieldCost = fieldCost;
        MaxFieldCost = maxFieldCost;
    }

    /// <summary>The query's field cost, when the server reported it.</summary>
    public double? FieldCost { get; }

    /// <summary>The server's limit, when the server reported it.</summary>
    public double? MaxFieldCost { get; }

    private static string CreateMessage(double? fieldCost, double? maxFieldCost)
    {
        var cost = fieldCost != null && maxFieldCost != null
            ? $" (field cost {fieldCost.Value.ToString(CultureInfo.InvariantCulture)}, limit {maxFieldCost.Value.ToString(CultureInfo.InvariantCulture)})"
            : null;
        return $"The GraphQL server rejected the query for its cost{cost}, error {ErrorCode}. HotChocolate costs filter " +
               "and sort variables as the worst case: raise MaxFieldCost or set EnforceCostLimits = false in the " +
               "server's ModifyCostOptions.";
    }

    /// <summary>
    /// The cost error in a Linq2GraphQL exception, or null. An HTTP 400 carries the response body in its message
    /// (<c>GraphQueryRequestException</c>); an HTTP 200 carries parsed errors (<c>GraphQueryExecutionException</c>).
    /// The exceptions are recognized by name, so a standalone client's copy of the runtime works too.
    /// </summary>
    internal static GraphQLCostLimitException? From(Exception exception)
    {
        if (ClientRuntime.Is(exception.GetType(), ClientRuntime.GraphQueryExecutionException))
        {
            var extensions = ClientRuntime.ErrorExtensions(exception)
                .FirstOrDefault(e => e != null && e.Contains("code") && e["code"]?.ToString() == ErrorCode);
            return extensions == null
                ? null
                : new GraphQLCostLimitException(Number(extensions, "fieldCost"), Number(extensions, "maxFieldCost"), exception);
        }

        if (ClientRuntime.Is(exception.GetType(), ClientRuntime.GraphQueryRequestException) &&
            exception.Message.Contains($"\"{ErrorCode}\"", StringComparison.Ordinal))
        {
            return new GraphQLCostLimitException(Number(exception.Message, "fieldCost"), Number(exception.Message, "maxFieldCost"), exception);
        }

        return null;
    }

    private static double? Number(IDictionary extensions, string name) =>
        (extensions.Contains(name) ? extensions[name] : null) switch
        {
            JsonElement { ValueKind: JsonValueKind.Number } e => e.GetDouble(),
            IConvertible c => c.ToDouble(CultureInfo.InvariantCulture),
            _ => null
        };

    private static double? Number(string body, string name)
    {
        var match = NumberPattern().Matches(body).FirstOrDefault(m => m.Groups[1].Value == name);
        return match != null && double.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;
    }

    [GeneratedRegex(@"""(\w+)""\s*:\s*(-?[0-9.]+(?:[eE][+-]?[0-9]+)?)")]
    private static partial Regex NumberPattern();
}
