namespace TabBlazor
{
    public interface IFlagType
    {
        public string ClassName { get; }
        public int Width { get; }
        public int Height { get; }
        public string Elements { get; }
        public string TwoLetterCode { get;  }
        public string ThreeLetterCode { get;  }
    }

    public class TablerFlag : IFlagType
    {
        public TablerFlag(string elements, string twoLetterCode = null, string threeLetterCode = null)
        {
            Elements = elements;
            TwoLetterCode = twoLetterCode;
            ThreeLetterCode = threeLetterCode;
        }

        public int Width => 640;

        public int Height => 480;

        public string Elements { get;  }

        public string ClassName => "TablerFlag";

        public string TwoLetterCode { get; internal set; }
        public string ThreeLetterCode { get; internal set; }
    }
}
