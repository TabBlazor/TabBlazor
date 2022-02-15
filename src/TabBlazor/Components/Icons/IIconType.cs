using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabBlazor
{
    public interface IIconType
    {
        public double StrokeWidth { get;  }
        public bool Filled { get;  }
        public string Elements { get; }

    }

    public class MDIcon : IIconType
    {
        public MDIcon(string elements)
        {
            Elements = elements;
        }
        public double StrokeWidth => 0.1;

        public bool Filled => true;

        public string Elements { get; }
    }

    public class TablerIcon : IIconType
    {
        public TablerIcon(string elements)
        {
            Elements = elements;
        }
        public double StrokeWidth => 2;

        public bool Filled => false;

        public string Elements { get; }
    }


}
