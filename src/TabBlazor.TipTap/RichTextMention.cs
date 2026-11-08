namespace TabBlazor;

/// <summary>A match offered when the user types <c>@</c> in a <see cref="RichTextEditor"/>.</summary>
/// <param name="Id">Stored in the content as <c>data-id</c>; use it to find the person again, for example to notify them.</param>
/// <param name="Label">Shown in the list and in the content, after the <c>@</c>.</param>
public sealed record RichTextMention(string Id, string Label)
{
    /// <summary>A second line in the list, such as an email address or a team.</summary>
    public string? Description { get; init; }

    /// <summary>An image shown next to the label in the list.</summary>
    public string? AvatarUrl { get; init; }
}
