
namespace TabBlazor
{
    internal static class InternalIcons
    {
        public static IIconType X => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><line x1='18' y1='6' x2='6' y2='18' /><line x1='6' y1='6' x2='18' y2='18' />");
        public static IIconType Arrow_right => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><line x1='5' y1='12' x2='19' y2='12' /><line x1='13' y1='18' x2='19' y2='12' /><line x1='13' y1='6' x2='19' y2='12' /> ");
        public static IIconType Arrow_left => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><line x1='5' y1='12' x2='19' y2='12' /><line x1='5' y1='12' x2='11' y2='18' /><line x1='5' y1='12' x2='11' y2='6' /> ");
        public static IIconType Calendar => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><rect x='4' y='5' width='16' height='16' rx='2' /><line x1='16' y1='3' x2='16' y2='7' /><line x1='8' y1='3' x2='8' y2='7' /><line x1='4' y1='11' x2='20' y2='11' /><line x1='11' y1='15' x2='12' y2='15' /><line x1='12' y1='15' x2='12' y2='18' /> ");
        public static IIconType Search => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><circle cx='10' cy='10' r='7' /><line x1='21' y1='21' x2='15' y2='15' /> ");
        public static IIconType Edit => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M9 7h-3a2 2 0 0 0 -2 2v9a2 2 0 0 0 2 2h9a2 2 0 0 0 2 -2v-3' /><path d='M9 15h3l8.5 -8.5a1.5 1.5 0 0 0 -3 -3l-8.5 8.5v3' /><line x1='16' y1='5' x2='19' y2='8' /> ");
        public static IIconType Trash => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><line x1='4' y1='7' x2='20' y2='7' /><line x1='10' y1='11' x2='10' y2='17' /><line x1='14' y1='11' x2='14' y2='17' /><path d='M5 7l1 12a2 2 0 0 0 2 2h8a2 2 0 0 0 2 -2l1 -12' /><path d='M9 7v-3a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v3' /> ");
        public static IIconType Circle_minus => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><circle cx='12' cy='12' r='9' /><line x1='9' y1='12' x2='15' y2='12' /> ");
        public static IIconType Circle_plus => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><circle cx='12' cy='12' r='9' /><line x1='9' y1='12' x2='15' y2='12' /><line x1='12' y1='9' x2='12' y2='15' /> ");
        public static IIconType Alert_triangle => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M12 9v2m0 4v.01' /><path d='M5 19h14a2 2 0 0 0 1.84 -2.75l-7.1 -12.25a2 2 0 0 0 -3.5 0l-7.1 12.25a2 2 0 0 0 1.75 2.75' />");

        public static IIconType Sortable => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><polyline points='8 7 12 3 16 7' /><polyline points='8 17 12 21 16 17' /><line x1='12' y1='3' x2='12' y2='21' />");
        public static IIconType Sort_Desc => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><polyline points='8 17 12 21 16 17' /><line x1='12' y1='3' x2='12' y2='21' />");
        public static IIconType Sort_Asc => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><polyline points='8 7 12 3 16 7' /><line x1='12' y1='3' x2='12' y2='21' />");

        public static IIconType Chevron_double_left => new MDIcon(@"<path d='M18.41,7.41L17,6L11,12L17,18L18.41,16.59L13.83,12L18.41,7.41M12.41,7.41L11,6L5,12L11,18L12.41,16.59L7.83,12L12.41,7.41Z' />");
        public static IIconType Chevron_double_right => new MDIcon(@"<path d='M5.59,7.41L7,6L13,12L7,18L5.59,16.59L10.17,12L5.59,7.41M11.59,7.41L13,6L19,12L13,18L11.59,16.59L16.17,12L11.59,7.41Z' />");
        public static IIconType Chevron_left => new MDIcon(@"<path d='M15.41,16.58L10.83,12L15.41,7.41L14,6L8,12L14,18L15.41,16.58Z' />");
        public static IIconType Chevron_right => new MDIcon(@"<path d='M8.59,16.58L13.17,12L8.59,7.41L10,6L16,12L10,18L8.59,16.58Z' />");
        
        public static IIconType Chevron_down => new MDIcon(@"<path d='M7.41,8.58L12,13.17L16.59,8.58L18,10L12,16L6,10L7.41,8.58Z' />");
        public static IIconType Chevron_up => new MDIcon(@"<path d='M7.41,15.41L12,10.83L16.59,15.41L18,14L12,8L6,14L7.41,15.41Z' />");

        public static IIconType Refresh => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M20 11a8.1 8.1 0 0 0 -15.5 -2m-.5 -4v4h4' /><path d='M4 13a8.1 8.1 0 0 0 15.5 2m.5 4v-4h-4' />");
        public static IIconType Dots_vertical => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M12 12m-1 0a1 1 0 1 0 2 0a1 1 0 1 0 -2 0' /><path d='M12 19m-1 0a1 1 0 1 0 2 0a1 1 0 1 0 -2 0' /><path d='M12 5m-1 0a1 1 0 1 0 2 0a1 1 0 1 0 -2 0' />");
        public static IIconType Dots => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M5 12m-1 0a1 1 0 1 0 2 0a1 1 0 1 0 -2 0' /><path d='M12 12m-1 0a1 1 0 1 0 2 0a1 1 0 1 0 -2 0' /><path d='M19 12m-1 0a1 1 0 1 0 2 0a1 1 0 1 0 -2 0' />");
        public static IIconType Filter => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M4 4h16v2.172a2 2 0 0 1 -.586 1.414l-4.414 4.414v7l-6 2v-8.5l-4.48 -4.928a2 2 0 0 1 -.52 -1.345v-2.227z' />");
        public static IIconType Check => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M5 12l5 5l10 -10' />");
        public static IIconType Plus => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><line x1='12' y1='5' x2='12' y2='19' /><line x1='5' y1='12' x2='19' y2='12' />");
        public static IIconType List => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M9 6l11 0' /><path d='M9 12l11 0' /><path d='M9 18l11 0' /><path d='M5 6l0 .01' /><path d='M5 12l0 .01' /><path d='M5 18l0 .01' />");
        public static IIconType Table => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M3 5a2 2 0 0 1 2 -2h14a2 2 0 0 1 2 2v14a2 2 0 0 1 -2 2h-14a2 2 0 0 1 -2 -2v-14z' /><path d='M3 10h18' /><path d='M10 3v18' />");

        public static IIconType Bold => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M7 5h6a3.5 3.5 0 0 1 0 7h-6z' /><path d='M13 12h1a3.5 3.5 0 0 1 0 7h-7v-7' />");
        public static IIconType Italic => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M11 5l6 0' /><path d='M7 19l6 0' /><path d='M14 5l-4 14' />");
        public static IIconType Heading => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M7 12h10' /><path d='M7 5v14' /><path d='M17 5v14' /><path d='M15 19h4' /><path d='M15 5h4' /><path d='M5 19h4' /><path d='M5 5h4' />");
        public static IIconType List_numbers => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M11 6h9' /><path d='M11 12h9' /><path d='M12 18h8' /><path d='M4 16a2 2 0 1 1 4 0c0 .591 -.5 1 -1 1.5l-3 2.5h4' /><path d='M6 10v-6l-2 2' />");
        public static IIconType Blockquote => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M6 15h15' /><path d='M21 19h-15' /><path d='M15 11h6' /><path d='M21 7h-6' /><path d='M9 9h1a1 1 0 1 1 -1 1v-2.5a2 2 0 0 1 2 -2' /><path d='M3 9h1a1 1 0 1 1 -1 1v-2.5a2 2 0 0 1 2 -2' />");
        public static IIconType Code => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M7 8l-4 4l4 4' /><path d='M17 8l4 4l-4 4' /><path d='M14 4l-4 16' />");
        public static IIconType Link => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M9 15l6 -6' /><path d='M11 6l.463 -.536a5 5 0 0 1 7.071 7.072l-.534 .464' /><path d='M13 18l-.397 .534a5.068 5.068 0 0 1 -7.127 0a4.972 4.972 0 0 1 0 -7.071l.524 -.463' />");
        public static IIconType Photo => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M15 8h.01' /><path d='M3 6a3 3 0 0 1 3 -3h12a3 3 0 0 1 3 3v12a3 3 0 0 1 -3 3h-12a3 3 0 0 1 -3 -3v-12z' /><path d='M3 16l5 -5c.928 -.893 2.072 -.893 3 0l5 5' /><path d='M14 14l1 -1c.928 -.893 2.072 -.893 3 0l3 3' />");
        public static IIconType Paperclip => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M15 7l-6.5 6.5a1.5 1.5 0 0 0 3 3l6.5 -6.5a3 3 0 0 0 -6 -6l-6.5 6.5a4.5 4.5 0 0 0 9 9l6.5 -6.5' />");
        public static IIconType Align_left => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M4 6l16 0' /><path d='M4 12l10 0' /><path d='M4 18l14 0' />");
        public static IIconType Align_center => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M4 6l16 0' /><path d='M8 12l8 0' /><path d='M6 18l12 0' />");
        public static IIconType Align_right => new TablerIcon(@"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M4 6l16 0' /><path d='M10 12l10 0' /><path d='M6 18l14 0' />");




    }
}
