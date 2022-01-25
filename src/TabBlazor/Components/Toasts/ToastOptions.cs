using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabBlazor
{
   public class ToastOptions
    {
        /// <summary>
        /// Delay in Seconds
        /// Set 0 to show it until manually removed
        /// </summary>
        public int Delay { get; set; } = 3;
        public bool ShowHeader { get; set; } = true;
        public bool ShowProgress { get; set; } = true;
        public ToastPosition Position { get; set; } = ToastPosition.TopRight;
        public bool AutoClose => Delay > 0;
    }

    public enum ToastPosition
    {
        TopRight,
        TopLeft,
        BottomRight,
        BottomLeft
    }
}
