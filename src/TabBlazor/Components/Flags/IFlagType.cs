namespace TabBlazor
{
    public interface IFlagType
    {
        public string ClassName { get; }
        public int Width { get; }
        public int Height { get; }
        public string Elements { get; }

        public Country Country { get; set; }
    }

    public class TablerFlag : IFlagType
    {
        
        public TablerFlag(string elements, int width, int height, Country country = null)
        {
            Elements = elements;
            Country = country;
            Width = width;
            Height = height;
        }

        public int Width { get; set; }

        public int Height { get; set; }

        public string Elements { get;  }
        public string Viewbox { get; }

        public string ClassName => "TablerFlag";

        public Country Country { get; set; }
    }
}
