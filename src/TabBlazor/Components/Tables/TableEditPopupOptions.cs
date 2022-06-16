using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabBlazor
{
    public class TableEditPopupOptions<TItem>
    {
        public string Title { get; set; } 
        public ModalOptions ModalOptions { get; set; }

        public TItem CurrentEditItem { get; internal set; }
        public bool IsAddInProgress { get; internal set; }
    }
}
