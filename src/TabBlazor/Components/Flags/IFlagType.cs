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
        public TablerFlag(string elements)
        {
            Elements = elements;
        }

        public TablerFlag(string elements, Country country = null)
        {
            Elements = elements;
            Country = country;
        }

        public int Width => 640;

        public int Height => 480;

        public string Elements { get;  }

        public string ClassName => "TablerFlag";

        public Country Country { get; set; }
    }
}
