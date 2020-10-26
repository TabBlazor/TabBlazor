using System;
using System.Linq;
using System.Text.RegularExpressions;
using Highlight.Patterns;

namespace Highlight.Engines
{
    public abstract class Engine : IEngine
    {
        private const RegexOptions DefaultRegexOptions = RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace;

        public string Highlight(Definition definition, string input)
        {
            if (definition == null) {
                throw new ArgumentNullException("definition");
            }

            var output = PreHighlight(definition, input);
            output = HighlightUsingRegex(definition, output);
            output = PostHighlight(definition, output);

            return output;
        }

        protected virtual string PreHighlight(Definition definition, string input)
        {
            return input;
        }

        protected virtual string PostHighlight(Definition definition, string input)
        {
            return input;
        }

        private string HighlightUsingRegex(Definition definition, string input)
        {
            var regexOptions = GetRegexOptions(definition);
            var evaluator = GetMatchEvaluator(definition);
            var regexPattern = definition.GetRegexPattern();
            var output = Regex.Replace(input, regexPattern, evaluator, regexOptions);

            return output;
        }

        private RegexOptions GetRegexOptions(Definition definition)
        {
            if (definition.CaseSensitive) {
                return DefaultRegexOptions | RegexOptions.IgnoreCase;
            }

            return DefaultRegexOptions;
        }

        private string ElementMatchHandler(Definition definition, Match match)
        {
            if (definition == null) {
                throw new ArgumentNullException("definition");
            }
            if (match == null) {
                throw new ArgumentNullException("match");
            }

            var pattern = definition.Patterns.First(x => match.Groups[x.Key].Success).Value;
            if (pattern != null) {
                if (pattern is BlockPattern) {
                    return ProcessBlockPatternMatch(definition, (BlockPattern) pattern, match);
                }
                if (pattern is MarkupPattern) {
                    return ProcessMarkupPatternMatch(definition, (MarkupPattern) pattern, match);
                }
                if (pattern is WordPattern) {
                    return ProcessWordPatternMatch(definition, (WordPattern) pattern, match);
                }
            }

            return match.Value;
        }

        private MatchEvaluator GetMatchEvaluator(Definition definition)
        {
            return match => ElementMatchHandler(definition, match);
        }

        protected abstract string ProcessBlockPatternMatch(Definition definition, BlockPattern pattern, Match match);
        protected abstract string ProcessMarkupPatternMatch(Definition definition, MarkupPattern pattern, Match match);
        protected abstract string ProcessWordPatternMatch(Definition definition, WordPattern pattern, Match match);
    }
}