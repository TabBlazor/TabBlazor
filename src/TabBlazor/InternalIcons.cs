using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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


        // public static string X { get => @"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><line x1='18' y1='6' x2='6' y2='18' /><line x1='6' y1='6' x2='18' y2='18' /> "; }
        //public static string Arrow_right { get => @"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><line x1='5' y1='12' x2='19' y2='12' /><line x1='13' y1='18' x2='19' y2='12' /><line x1='13' y1='6' x2='19' y2='12' /> "; }
        //public static string Arrow_left { get => @"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><line x1='5' y1='12' x2='19' y2='12' /><line x1='5' y1='12' x2='11' y2='18' /><line x1='5' y1='12' x2='11' y2='6' /> "; }
        //public static string Calendar { get => @"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><rect x='4' y='5' width='16' height='16' rx='2' /><line x1='16' y1='3' x2='16' y2='7' /><line x1='8' y1='3' x2='8' y2='7' /><line x1='4' y1='11' x2='20' y2='11' /><line x1='11' y1='15' x2='12' y2='15' /><line x1='12' y1='15' x2='12' y2='18' /> "; }
        //public static string Search { get => @"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><circle cx='10' cy='10' r='7' /><line x1='21' y1='21' x2='15' y2='15' /> "; }
        //public static string Edit { get => @"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M9 7h-3a2 2 0 0 0 -2 2v9a2 2 0 0 0 2 2h9a2 2 0 0 0 2 -2v-3' /><path d='M9 15h3l8.5 -8.5a1.5 1.5 0 0 0 -3 -3l-8.5 8.5v3' /><line x1='16' y1='5' x2='19' y2='8' /> "; }
        //public static string Trash { get => @"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><line x1='4' y1='7' x2='20' y2='7' /><line x1='10' y1='11' x2='10' y2='17' /><line x1='14' y1='11' x2='14' y2='17' /><path d='M5 7l1 12a2 2 0 0 0 2 2h8a2 2 0 0 0 2 -2l1 -12' /><path d='M9 7v-3a1 1 0 0 1 1 -1h4a1 1 0 0 1 1 1v3' /> "; }
        //public static string Circle_minus { get => @"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><circle cx='12' cy='12' r='9' /><line x1='9' y1='12' x2='15' y2='12' /> "; }
        //public static string Circle_plus { get => @"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><circle cx='12' cy='12' r='9' /><line x1='9' y1='12' x2='15' y2='12' /><line x1='12' y1='9' x2='12' y2='15' /> "; }
        //public static string Alert_triangle { get => @"<path stroke='none' d='M0 0h24v24H0z' fill='none' /><path d='M12 9v2m0 4v.01' /><path d='M5 19h14a2 2 0 0 0 1.84 -2.75l-7.1 -12.25a2 2 0 0 0 -3.5 0l-7.1 12.25a2 2 0 0 0 1.75 2.75' />"; }

    }
}
