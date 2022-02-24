
namespace TabBlazor
{
    public interface IIcon
    {
        public double StrokeWidth { get;  }
        public bool Filled { get;  }
        public string Elements { get; }

        public string ClassName { get; }


    }

    public class MDIcon : IIcon
    {
        public MDIcon(string elements)
        {
            Elements = elements;
        }
        public double StrokeWidth => 0.1;
        public bool Filled => true;
        public string Elements { get; }
        public string ClassName => "MDIcon";
    }

    public class TablerIcon : IIcon
    {
        public TablerIcon(string elements)
        {
            Elements = elements;
        }
        public double StrokeWidth => 2;
        public bool Filled => false;
        public string Elements { get; }
        public string ClassName => "TablerIcon";
    }


}
