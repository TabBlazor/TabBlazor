using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabBlazor
{
   public class Toast
    {

        public Toast()
        {}

        public Toast(string title, string subTitle, string message)
        {
            Title = title;
            SubTitle = subTitle;
            Message = message;
        }

        public Toast(string title, string subTitle, DynamicComponent component)
        {
            Title = title;
            SubTitle = subTitle;
            Component = component;
        }

        public string Title { get; set; }
        public string SubTitle { get; set; }
        public string Message { get; set; }
        public ToastOptions Options { get; set; } = new ToastOptions();
        public DynamicComponent Component { get; set; }
    }
}
