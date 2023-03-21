
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

       


    }
}
