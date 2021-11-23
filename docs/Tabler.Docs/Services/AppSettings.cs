using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TabBlazor;

namespace Tabler.Docs.Services
{
    public class AppSettings
    {
        public bool DarkMode { get; set; }
        public NavbarDirection NavbarDirection { get; set; } = NavbarDirection.Vertical;
        public NavbarBackground NavbarBackground { get; set; } = NavbarBackground.Dark;
        
    }
}
