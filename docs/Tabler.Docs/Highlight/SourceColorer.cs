using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Tabler.Docs.Highlight
{
	/// <summary>
	/// A basic implementation of a pretty-printer or syntax highlighter for C# soure code.
	/// </summary>
	public class SourceColorer
	{
		private string _commentCssClass;
		private string _keywordCssClass;
		private string _quotesCssClass;
		private string _typeCssClass;
		private bool _addStyleDefinition;
		private HashSet<string> _keywords;
		private bool _addPreTags;

		/// <summary>
		/// Gets the list of reserved words/keywords.
		/// </summary>
		public HashSet<string> Keywords
		{
			get { return _keywords; }
		}

		/// <summary>
		/// Gets or sets the CSS class used for comments. The default is 'comment'.
		/// </summary>
		public string CommentCssClass
		{
			get { return _commentCssClass; }
			set { _commentCssClass = value; }
		}

		/// <summary>
		/// Gets or sets the CSS class used for keywords. The default is 'keyword'.
		/// </summary>
		public string KeywordCssClass
		{
			get { return _keywordCssClass; }
			set { _keywordCssClass = value; }
		}

		/// <summary>
		/// Gets or sets the CSS class used for string quotes. The default is 'quotes'.
		/// </summary>
		public string QuotesCssClass
		{
			get { return _quotesCssClass; }
			set { _quotesCssClass = value; }
		}

		/// <summary>
		/// Gets or sets the CSS class used for types. The default is 'type'.
		/// </summary>
		public string TypeCssClass
		{
			get { return _typeCssClass; }
			set { _typeCssClass = value; }
		}

		/// <summary>
		/// Whether to add the CSS style definition to the top of the highlighted code.
		/// </summary>
		public bool AddStyleDefinition
		{
			get { return _addStyleDefinition; }
			set { _addStyleDefinition = value; }
		}

		/// <summary>
		/// Whether to insert opening and closing pre tags around the highlighted code.
		/// </summary>
		public bool AddPreTags
		{
			get { return _addPreTags; }
			set { _addPreTags = value; }
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SourceColorer"/> class.
		/// </summary>
		public SourceColorer()
		{
			_addStyleDefinition = true;
			_commentCssClass = "comment";
			_keywordCssClass = "keyword";
			_quotesCssClass = "quotes";
			_typeCssClass = "type";
			_keywords = new HashSet<string>()
		{
			"static", "using", "true", "false","new",
			"namespace", "void", "private", "public",
			"bool", "string", "return", "class","internal",
			"const", "readonly", "int", "double","lock",
			"float", "if", "else", "foreach", "for","var",
			"get","set","byte\\[\\]","char\\[\\]","int\\[\\]","string\\[\\]" // dumb array matching. Escaped as [] is regex syntax
		};
		}

		/// <summary>
		/// Highlights the specified source code and returns it as stylised HTML.
		/// </summary>
		/// <param name="source">The source code.</param>
		/// <returns></returns>
		public string Highlight(string source)
		{
			StringBuilder builder = new StringBuilder();
			if (AddStyleDefinition)
			{
				builder.Append("<style>");
				builder.AppendFormat(".{0}  {{ color: #0000FF  }} ", KeywordCssClass);
				builder.AppendFormat(".{0}  {{ color: #2B91AF  }} ", TypeCssClass);
				builder.AppendFormat(".{0}  {{ color: green    }} ", CommentCssClass);
				builder.AppendFormat(".{0}  {{ color: maroon   }} ", QuotesCssClass);
				builder.Append("</style>");
			}

			if (AddPreTags)
				builder.Append("<pre>");

			builder.Append(HighlightSource(source));

			if (AddPreTags)
				builder.Append("</pre>");

			return builder.ToString();
		}

		/// <summary>
		/// Occurs when the source code is highlighted, after any style (CSS) definitions are added.
		/// </summary>
		/// <param name="content">The source code content.</param>
		/// <returns>The highlighted source code.</returns>
		protected virtual string HighlightSource(string content)
		{
			if (string.IsNullOrEmpty(CommentCssClass))
				throw new InvalidOperationException("The CommentCssClass should not be null or empty");
			if (string.IsNullOrEmpty(KeywordCssClass))
				throw new InvalidOperationException("The KeywordCssClass should not be null or empty");
			if (string.IsNullOrEmpty(QuotesCssClass))
				throw new InvalidOperationException("The CommentCssClass should not be null or empty");
			if (string.IsNullOrEmpty(TypeCssClass))
				throw new InvalidOperationException("The TypeCssClass should not be null or empty");

			// Some fairly secure token placeholders
			const string COMMENTS_TOKEN = "`````";
			const string MULTILINECOMMENTS_TOKEN = "~~~~~";
			const string QUOTES_TOKEN = "Â¬Â¬Â¬Â¬Â¬";

			// Remove /* */ quotes, taken from ostermiller.org
			Regex regex = new Regex(@"/\*([^*]|[\r\n]|(\*+([^*/]|[\r\n])))*\*+/", RegexOptions.Singleline);
			List<string> multiLineComments = new List<string>();
			if (regex.IsMatch(content))
			{
				foreach (Match item in regex.Matches(content))
				{
					if (!multiLineComments.Contains(item.Value))
						multiLineComments.Add(item.Value);
				}
			}

			for (int i = 0; i < multiLineComments.Count; i++)
			{
				content = content.ReplaceToken(multiLineComments[i], MULTILINECOMMENTS_TOKEN, i);
			}

			// Remove the quotes first, so they don't get highlighted
			List<string> quotes = new List<string>();
			bool onEscape = false;
			bool onComment1 = false;
			bool onComment2 = false;
			bool inQuotes = false;
			int start = -1;
			for (int i = 0; i < content.Length; i++)
			{
				if (content[i] == '/' && !inQuotes && !onComment1)
					onComment1 = true;
				else if (content[i] == '/' && !inQuotes && onComment1)
					onComment2 = true;
				else if (content[i] == '"' && !onEscape && !onComment2)
				{
					inQuotes = true; // stops cases of: var s = "// I'm a comment";
					if (start > -1)
					{
						string quote = content.Substring(start, i - start + 1);
						if (!quotes.Contains(quote))
							quotes.Add(quote);
						start = -1;
						inQuotes = false;
					}
					else
					{
						start = i;
					}
				}
				else if (content[i] == '\\' || content[i] == '\'')
					onEscape = true;
				else if (content[i] == '\n')
				{
					onComment1 = false;
					onComment2 = false;
				}
				else
				{
					onEscape = false;
				}
			}

			for (int i = 0; i < quotes.Count; i++)
			{
				content = content.ReplaceToken(quotes[i], QUOTES_TOKEN, i);
			}

			// Remove the comments next, so they don't get highlighted
			regex = new Regex("(/{2,3}.+)\n", RegexOptions.Multiline);
			List<string> comments = new List<string>();
			if (regex.IsMatch(content))
			{
				foreach (Match item in regex.Matches(content))
				{
					if (!comments.Contains(item.Value + "\n"))
						comments.Add(item.Value);
				}
			}

			for (int i = 0; i < comments.Count; i++)
			{
				content = content.ReplaceToken(comments[i], COMMENTS_TOKEN, i);
			}

			// Highlight single quotes
			content = Regex.Replace(content, "('.{1,2}')", "<span class=\"quote\">$1</span>", RegexOptions.Singleline);

			// Highlight class names based on the logic: {space OR start of line OR >}{1 capital){alphanumeric}{space}
			regex = new Regex(@"((?:\s|^)[A-Z]\w+(?:\s))", RegexOptions.Singleline);
			List<string> highlightedClasses = new List<string>();
			if (regex.IsMatch(content))
			{
				foreach (Match item in regex.Matches(content))
				{
					string val = item.Groups[1].Value;
					if (!highlightedClasses.Contains(val))
						highlightedClasses.Add(val);
				}
			}

			for (int i = 0; i < highlightedClasses.Count; i++)
			{
				content = content.ReplaceWithCss(highlightedClasses[i], TypeCssClass);
			}

			// Pass 2. Doing it in N passes due to my inferior regex knowledge of back/forwardtracking.
			// This does {space or [}{1 capital){alphanumeric}{]}
			regex = new Regex(@"(?:\s|\[)([A-Z]\w+)(?:\])", RegexOptions.Singleline);
			highlightedClasses = new List<string>();
			if (regex.IsMatch(content))
			{
				foreach (Match item in regex.Matches(content))
				{
					string val = item.Groups[1].Value;
					if (!highlightedClasses.Contains(val))
						highlightedClasses.Add(val);
				}
			}

			for (int i = 0; i < highlightedClasses.Count; i++)
			{
				content = content.ReplaceWithCss(highlightedClasses[i], TypeCssClass);
			}

			// Pass 3. Generics
			regex = new Regex(@"(?:\s|\[|\()([A-Z]\w+(?:<|&lt;))", RegexOptions.Singleline);
			highlightedClasses = new List<string>();
			if (regex.IsMatch(content))
			{
				foreach (Match item in regex.Matches(content))
				{
					string val = item.Groups[1].Value;
					if (!highlightedClasses.Contains(val))
						highlightedClasses.Add(val);
				}
			}

			for (int i = 0; i < highlightedClasses.Count; i++)
			{
				string val = highlightedClasses[i];
				val = val.Replace("<", "").Replace("&lt;", "");
				content = content.ReplaceWithCss(highlightedClasses[i], val, "&lt;", TypeCssClass);
			}

			// Pass 4. new keyword with a type
			regex = new Regex(@"new\s+([A-Z]\w+)(?:\()", RegexOptions.Singleline);
			highlightedClasses = new List<string>();
			if (regex.IsMatch(content))
			{
				foreach (Match item in regex.Matches(content))
				{
					string val = item.Groups[1].Value;
					if (!highlightedClasses.Contains(val))
						highlightedClasses.Add(val);
				}
			}

			// Replace the highlighted classes
			for (int i = 0; i < highlightedClasses.Count; i++)
			{
				content = content.ReplaceWithCss(highlightedClasses[i], TypeCssClass);
			}

			// Highlight keywords
			foreach (string keyword in _keywords)
			{
				Regex regexKeyword = new Regex("(" + keyword + @")(>|&gt;|\s|\n|;|<)", RegexOptions.Singleline);
				content = regexKeyword.Replace(content, "<span class=\"keyword\">$1</span>$2");
			}

			// Shove the multiline comments back in
			for (int i = 0; i < multiLineComments.Count; i++)
			{
				content = content.ReplaceTokenWithCss(multiLineComments[i], MULTILINECOMMENTS_TOKEN, i, CommentCssClass);
			}

			// Shove the quotes back in
			for (int i = 0; i < quotes.Count; i++)
			{
				content = content.ReplaceTokenWithCss(quotes[i], QUOTES_TOKEN, i, QuotesCssClass);
			}

			// Shove the single line comments back in
			for (int i = 0; i < comments.Count; i++)
			{
				string comment = comments[i];
				// Add quotes back in
				for (int n = 0; n < quotes.Count; n++)
				{
					comment = comment.Replace(string.Format("{0}{1}{0}", QUOTES_TOKEN, n), quotes[n]);
				}
				content = content.ReplaceTokenWithCss(comment, COMMENTS_TOKEN, i, CommentCssClass);
			}
			return content;
		}
	}

	public static class MoreExtensions
	{
		public static string ReplaceWithCss(this string content, string source, string cssClass)
		{
			return content.Replace(source, string.Format("<span class=\"{0}\">{1}</span>", cssClass, source));
		}

		public static string ReplaceWithCss(this string content, string source, string replacement, string cssClass)
		{
			return content.Replace(source, string.Format("<span class=\"{0}\">{1}</span>", cssClass, replacement));
		}

		public static string ReplaceWithCss(this string content, string source, string replacement, string suffix, string cssClass)
		{
			return content.Replace(source, string.Format("<span class=\"{0}\">{1}</span>{2}", cssClass, replacement, suffix));
		}

		public static string ReplaceTokenWithCss(this string content, string source, string token, int counter, string cssClass)
		{
			string formattedToken = String.Format("{0}{1}{0}", token, counter);
			return content.Replace(formattedToken, string.Format("<span class=\"{0}\">{1}</span>", cssClass, source));
		}

		public static string ReplaceToken(this string content, string source, string token, int counter)
		{
			string formattedToken = String.Format("{0}{1}{0}", token, counter);
			return content.Replace(source, formattedToken);
		}
	}
}
