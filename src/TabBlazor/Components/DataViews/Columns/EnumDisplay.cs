#nullable enable
using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace TabBlazor;

/// <summary>Display names for enum values: <see cref="DisplayAttribute"/>, then <see cref="DescriptionAttribute"/>, then the name.</summary>
public static class EnumDisplay
{
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

    public static string GetName(Enum value) => Names.GetOrAdd(value, static v =>
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
