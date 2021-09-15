using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabBlazor.Components.Modals.Standard
{
    public class ConfirmOptions
    {
        public string MainText { get; set; }
        public string SubText { get; set; }
        public string IconElements { get; set; }

        public string CancelText { get; set; } = "Cancel";
        public string OkText { get; set; } = "Ok";

    }
}
