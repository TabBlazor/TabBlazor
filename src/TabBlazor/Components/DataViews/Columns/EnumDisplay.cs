#nullable enable
using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace TabBlazor;

/// <summary>
/// Display names for enum values: <see cref="Formatter"/>, then <see cref="DisplayAttribute"/>, then
/// <see cref="DescriptionAttribute"/>, then the name.
/// </summary>
public static class EnumDisplay
{
    /// <summary>
    /// Names enum values app-wide, for enums that cannot carry attributes (generated GraphQL clients, for example).
    /// Return null to fall back to the attributes and the name. Set it once at startup; it is called on every use, so
    /// it can follow the current culture. DataView uses it for cells, filter options, filter chips, editors and the
    /// in-memory search.
    /// </summary>
    /// <example><c>EnumDisplay.Formatter = e =&gt; e is SystemKind.SapS4 ? "SAP S/4HANA" : null;</c></example>
    public static Func<Enum, string?>? Formatter { get; set; }

    private static readonly ConcurrentDictionary<Enum, string> Names = new();
    private static readonly ConcurrentDictionary<Type, Array> Values = new();

    /// <summary>
    /// The values offered in filters and editors. Members named with a leading <c>__</c> are left out: GraphQL reserves
    /// that prefix, so such a member is a client-side placeholder (Linq2GraphQL's <c>__Unknown</c>) that a server never
    /// accepts.
    /// </summary>
    public static Array GetValues(Type enumType) => Values.GetOrAdd(enumType, static t =>
    {
        var values = Enum.GetValues(t).Cast<Enum>().Where(v => !v.ToString().StartsWith("__", StringComparison.Ordinal)).ToArray();
        var result = Array.CreateInstance(t, values.Length);
        Array.Copy(values, result, values.Length);
        return result;
    });

    public static string GetName(Enum value) => Formatter?.Invoke(value) ?? Names.GetOrAdd(value, static v =>
    {
        var name = v.ToString();
        var field = v.GetType().GetField(name, BindingFlags.Public | BindingFlags.Static);
        if (field == null)
        {
            return name;
        }

        return field.GetCustomAttribute<DisplayAttribute>()?.GetName()
               ?? field.GetCustomAttribute<DescriptionAttribute>()?.Description
               ?? name;
    });
}
