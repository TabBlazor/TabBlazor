
namespace TabBlazor
{
    public interface IIcon
    {
        public double StrokeWidth { get;  }
        public bool Filled { get;  }
        public string Elements { get; }

        public string Provider { get; }


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
        public string Provider => "Material Design";
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
        public string Provider => "Tabler";
    }


}
