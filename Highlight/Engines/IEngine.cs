using Highlight.Patterns;

namespace Highlight.Engines
{
    public interface IEngine
    {
        string Highlight(Definition definition, string input);
    }
}