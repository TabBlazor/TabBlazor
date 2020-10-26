namespace Highlight.Patterns
{
    public sealed class MarkupPattern : Pattern
    {
        public bool HighlightAttributes { get; set; }
        public ColorPair BracketColors { get; set; }
        public ColorPair AttributeNameColors { get; set; }
        public ColorPair AttributeValueColors { get; set; }

        public MarkupPattern(string name, Style style, bool highlightAttributes, ColorPair bracketColors, ColorPair attributeNameColors, ColorPair attributeValueColors)
            : base(name, style)
        {
            HighlightAttributes = highlightAttributes;
            BracketColors = bracketColors;
            AttributeNameColors = attributeNameColors;
            AttributeValueColors = attributeValueColors;
        }

        public override string GetRegexPattern()
        {
            return @"
                (?'openTag'&lt;\??/?)
                (?'ws1'\s*?)
                (?'tagName'[\w\:]+)
                (?>
                    (?!=[\/\?]?&gt;)
                    (?'ws2'\s*?)
                    (?'attribute'
                        (?'attribName'[\w\:-]+)
                        (?'attribValue'(\s*=\s*(?:&\#39;.*?&\#39;|&quot;.*?&quot;|\w+))?)
                        # (?:(?'ws3'\s*)(?'attribSign'=)(?'ws4'\s*))
                        # (?'attribValue'(?:&\#39;.*?&\#39;|&quot;.*?&quot;|\w+))
                    )
                )*
                (?'ws5'\s*?)
                (?'closeTag'[\/\?]?&gt;)
            ";
        }
    }
}