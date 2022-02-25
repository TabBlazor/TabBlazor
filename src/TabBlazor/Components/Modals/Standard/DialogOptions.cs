using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabBlazor.Components.Modals
{
    public class DialogOptions
    {
        public string MainText { get; set; }
        public string SubText { get; set; }
        public IIconType IconType { get; set; }

        public string CancelText { get; set; } = "Cancel";
        public string OkText { get; set; } = "Ok";

        public TablerColor StatusColor = TablerColor.Default;

    }
}
