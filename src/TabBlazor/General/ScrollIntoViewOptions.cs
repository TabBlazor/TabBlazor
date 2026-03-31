using System.Text.Json;
using System.Text.Json.Serialization;

namespace TabBlazor;

public class ScrollIntoViewOptions
{
    [JsonPropertyName("behavior")]
    public ScrollBehavior? Behavior { get; set; }

    [JsonPropertyName("block")]
    public ScrollLogicalPosition? Block { get; set; }

    [JsonPropertyName("inline")]
    public ScrollLogicalPosition? Inline { get; set; }

    [JsonPropertyName("container")]
    public ScrollContainer? Container { get; set; }
}

[JsonConverter(typeof(LowerCaseEnumConverter))]
public enum ScrollContainer
{
    Parent,
    Viewport,
    Nearest
}

[JsonConverter(typeof(LowerCaseEnumConverter))]
public enum ScrollBehavior
{
    Auto,
    Instant,
    Smooth
}

[JsonConverter(typeof(LowerCaseEnumConverter))]
public enum ScrollLogicalPosition
{
    Start,
    Center,
    End,
    Nearest
}

internal class LowerCaseEnumConverter() : JsonStringEnumConverter(JsonNamingPolicy.CamelCase);
