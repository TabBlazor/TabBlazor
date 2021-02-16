using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabBlazor
{
   public class ToastOptions
    {
        public int Delay { get; set; } = 3000;
        public bool ShowHeader { get; set; } = true;
        public bool ShowProgress { get; set; } = true;
        public bool AutoClose => Delay > 0;
    }
}
