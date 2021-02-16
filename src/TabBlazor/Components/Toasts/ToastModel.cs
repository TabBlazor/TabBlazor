using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabBlazor
{
   public class ToastModel
    {

        public ToastModel()
        {}

        public ToastModel(string title, string subTitle, string message, ToastOptions options = null)
        {
            Title = title;
            SubTitle = subTitle;
            Message = message;
            Options = options ?? new ToastOptions();
        }

        public ToastModel(string title, string subTitle, DynamicComponent component, ToastOptions options = null)
        {
            Title = title;
            SubTitle = subTitle;
            Component = component;
            Options = options ?? new ToastOptions();
        }

        public string Title { get; set; }
        public string SubTitle { get; set; }
        public string Message { get; set; }
        public ToastOptions Options { get; set; } = new ToastOptions();
        public DynamicComponent Component { get; set; }
    }
}
