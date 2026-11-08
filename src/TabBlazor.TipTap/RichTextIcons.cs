namespace TabBlazor;

/// <summary>Tabler icons the rich text toolbar uses that TabBlazor itself does not ship.</summary>
internal static class RichTextIcons
{
    public static IIconType Underline => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M7 5v5a5 5 0 0 0 10 0v-5' /><path d='M5 19h14' />", false);
    public static IIconType Strikethrough => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M5 12l14 0' /><path d='M16 6.5a4 2 0 0 0 -4 -1.5h-1a3.5 3.5 0 0 0 0 7h2a3.5 3.5 0 0 1 0 7h-1.5a4 2 0 0 1 -4 -1.5' />", false);
    public static IIconType Highlight => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M3 19h4l10.5 -10.5a2.828 2.828 0 1 0 -4 -4l-10.5 10.5v4' /><path d='M12.5 5.5l4 4' /><path d='M4.5 13.5l4 4' /><path d='M21 15v4h-8l4 -4z' />", false);
    public static IIconType List_check => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M3.5 5.5l1.5 1.5l2.5 -2.5' /><path d='M3.5 11.5l1.5 1.5l2.5 -2.5' /><path d='M3.5 17.5l1.5 1.5l2.5 -2.5' /><path d='M11 6l9 0' /><path d='M11 12l9 0' /><path d='M11 18l9 0' />", false);
    public static IIconType Source_code => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M14.5 4h2.5a3 3 0 0 1 3 3v10a3 3 0 0 1 -3 3h-10a3 3 0 0 1 -3 -3v-5' /><path d='M6 5l-2 2l2 2' /><path d='M10 9l2 -2l-2 -2' />", false);
    public static IIconType Separator_horizontal => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M4 12l16 0' /><path d='M8 8l4 -4l4 4' /><path d='M16 16l-4 4l-4 -4' />", false);
    public static IIconType Arrow_back_up => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M9 14l-4 -4l4 -4' /><path d='M5 10h11a4 4 0 1 1 0 8h-1' />", false);
    public static IIconType Arrow_forward_up => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M15 14l4 -4l-4 -4' /><path d='M19 10h-11a4 4 0 1 0 0 8h1' />", false);
    public static IIconType Row_insert_bottom => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M20 6v4a1 1 0 0 1 -1 1h-14a1 1 0 0 1 -1 -1v-4a1 1 0 0 1 1 -1h14a1 1 0 0 1 1 1z' /><path d='M12 15l0 4' /><path d='M14 17l-4 0' />", false);
    public static IIconType Column_insert_right => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M6 4h4a1 1 0 0 1 1 1v14a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1v-14a1 1 0 0 1 1 -1z' /><path d='M15 12l4 0' /><path d='M17 10l0 4' />", false);
    public static IIconType Row_remove => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M20 6v4a1 1 0 0 1 -1 1h-14a1 1 0 0 1 -1 -1v-4a1 1 0 0 1 1 -1h14a1 1 0 0 1 1 1z' /><path d='M10 16l4 4' /><path d='M10 20l4 -4' />", false);
    public static IIconType Column_remove => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M6 4h4a1 1 0 0 1 1 1v14a1 1 0 0 1 -1 1h-4a1 1 0 0 1 -1 -1v-14a1 1 0 0 1 1 -1z' /><path d='M16 10l4 4' /><path d='M16 14l4 -4' />", false);
    public static IIconType Table_off => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M7 3h12a2 2 0 0 1 2 2v12m-.585 3.413a1.994 1.994 0 0 1 -1.415 .587h-14a2 2 0 0 1 -2 -2v-14c0 -.55 .223 -1.05 .583 -1.412' /><path d='M3 10h7m4 0h7' /><path d='M10 3v3m0 4v11' /><path d='M3 3l18 18' />", false);
    public static IIconType Link_off => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M9 15l3 -3m2 -2l1 -1' /><path d='M11 6l.463 -.536a5 5 0 0 1 7.071 7.072l-.534 .464' /><path d='M3 3l18 18' /><path d='M13 18l-.397 .534a5.068 5.068 0 0 1 -7.127 0a4.972 4.972 0 0 1 0 -7.071l.524 -.463' />", false);
}
