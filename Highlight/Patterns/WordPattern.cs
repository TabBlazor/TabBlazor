using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Highlight.Patterns
{
    public sealed class WordPattern : Pattern
    {
        public IEnumerable<string> Words { get; private set; }

        public WordPattern(string name, Style style, IEnumerable<string> words)
            : base(name, style)
        {
            Words = words;
        }

        public override string GetRegexPattern()
        {
            var str = String.Empty;
            if (Words.Count() > 0) {
                var nonWords = GetNonWords();
                str = String.Format(@"(?<![\w{0}])(?=[\w{0}])({1})(?<=[\w{0}])(?![\w{0}])", nonWords, String.Join("|", Words.ToArray()));
            }

            return str;
        }

        private string GetNonWords()
        {
            var input = String.Join("", Words.ToArray());
            var list = new List<string>();
            foreach (var match in Regex.Matches(input, @"\W").Cast<Match>().Where(x => !list.Contains(x.Value))) {
                list.Add(match.Value);
            }

            return String.Join("", list.ToArray());
        }
    }
}