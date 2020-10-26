using System;
using Highlight.Configuration;
using Highlight.Engines;

namespace Highlight
{
    public class Highlighter
    {
        public IEngine Engine { get; set; }
        public IConfiguration Configuration { get; set; }

        public Highlighter(IEngine engine, IConfiguration configuration)
        {
            Engine = engine;
            Configuration = configuration;
        }

        public Highlighter(IEngine engine)
            : this(engine, new DefaultConfiguration())
        {
        }

        public string Highlight(string definitionName, string input)
        {
            if (definitionName == null) {
                throw new ArgumentNullException("definitionName");
            }

            if (Configuration.Definitions.ContainsKey(definitionName)) {
                var definition = Configuration.Definitions[definitionName];
                return Engine.Highlight(definition, input);
            }

            return input;
        }
    }
}