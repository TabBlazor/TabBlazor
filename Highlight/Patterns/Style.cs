using System.Drawing;

namespace Highlight.Patterns
{
    public class Style
    {
        public ColorPair Colors { get; private set; }
        public Font Font { get; private set; }

        public Style(ColorPair colors, Font font)
        {
            Colors = colors;
            Font = font;
        }
    }
}