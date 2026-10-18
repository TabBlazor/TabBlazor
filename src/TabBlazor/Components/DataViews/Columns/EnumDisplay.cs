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
