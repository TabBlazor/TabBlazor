namespace Highlight.Patterns
{
    public abstract class Pattern
    {
        public string Name { get; private set; }
        public Style Style { get; private set; }

        internal Pattern(string name, Style style)
        {
            Name = name;
            Style = style;
        }

        public abstract string GetRegexPattern();
    }
}