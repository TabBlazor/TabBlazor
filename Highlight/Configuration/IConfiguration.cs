using System.Collections.Generic;
using Highlight.Patterns;

namespace Highlight.Configuration
{
    public interface IConfiguration
    {
        IDictionary<string, Definition> Definitions { get; }
    }
}