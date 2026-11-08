using System;
using System.Text.Json;

namespace TabBlazor.Tests.Components
{
    public class RichTextJsonTests
    {
        private static string Doc(params object[] content) => JsonSerializer.Serialize(new { type = "doc", content });

        private static object P(params object[] content) => new { type = "paragraph", content };

        private static object T(string text, params object[] marks) => new { type = "text", text, marks };

        // Made by TipTap 3.32 in the browser with the editor's extensions: generateJSON, then generateHTML. The HTML is
        // TipTap's, without the table's colgroup and min-width, which only matter while editing.
        private const string TipTapJson = """
            {"type":"doc","content":[{"type":"heading","attrs":{"textAlign":"center","level":1},"content":[{"type":"text","text":"Title & \"q\" "},{"type":"text","marks":[{"type":"bold"}],"text":"x"}]},{"type":"paragraph","attrs":{"textAlign":null},"content":[{"type":"text","text":"Plain "},{"type":"text","marks":[{"type":"bold"}],"text":"b"},{"type":"text","text":" "},{"type":"text","marks":[{"type":"italic"}],"text":"i"},{"type":"text","text":" "},{"type":"text","marks":[{"type":"underline"}],"text":"u"},{"type":"text","text":" "},{"type":"text","marks":[{"type":"strike"}],"text":"s"},{"type":"text","text":" "},{"type":"text","marks":[{"type":"code"}],"text":"c<"},{"type":"text","text":" "},{"type":"text","marks":[{"type":"highlight"}],"text":"h"},{"type":"text","text":" "},{"type":"text","marks":[{"type":"link","attrs":{"href":"https://a.com/?x=1&y=2","target":null,"rel":"noopener noreferrer nofollow","class":null,"title":null}}],"text":"link"},{"type":"hardBreak"},{"type":"text","text":"line2 "},{"type":"mention","attrs":{"id":"7","label":"Anna","mentionSuggestionChar":"@"}}]},{"type":"paragraph","attrs":{"textAlign":null}},{"type":"blockquote","content":[{"type":"paragraph","attrs":{"textAlign":null},"content":[{"type":"text","text":"q"}]}]},{"type":"codeBlock","attrs":{"language":"js"},"content":[{"type":"text","text":"let a = 1 < 2;\nb"}]},{"type":"codeBlock","attrs":{"language":null},"content":[{"type":"text","text":"plain"}]},{"type":"bulletList","content":[{"type":"listItem","content":[{"type":"paragraph","attrs":{"textAlign":null},"content":[{"type":"text","text":"a"}]},{"type":"bulletList","content":[{"type":"listItem","content":[{"type":"paragraph","attrs":{"textAlign":null},"content":[{"type":"text","text":"n"}]}]}]}]}]},{"type":"orderedList","attrs":{"start":3,"type":null},"content":[{"type":"listItem","content":[{"type":"paragraph","attrs":{"textAlign":null},"content":[{"type":"text","text":"o"}]}]}]},{"type":"orderedList","attrs":{"start":1,"type":null},"content":[{"type":"listItem","content":[{"type":"paragraph","attrs":{"textAlign":null},"content":[{"type":"text","text":"o1"}]}]}]},{"type":"taskList","content":[{"type":"taskItem","attrs":{"checked":true},"content":[{"type":"paragraph","attrs":{"textAlign":null},"content":[{"type":"text","text":"done"}]}]},{"type":"taskItem","attrs":{"checked":false},"content":[{"type":"paragraph","attrs":{"textAlign":null},"content":[{"type":"text","text":"todo"}]}]}]},{"type":"horizontalRule"},{"type":"image","attrs":{"src":"https://x.com/a.png","alt":"A","title":"T","width":null,"height":null}},{"type":"table","content":[{"type":"tableRow","content":[{"type":"tableHeader","attrs":{"colspan":2,"rowspan":1,"colwidth":null,"align":null},"content":[{"type":"paragraph","attrs":{"textAlign":null},"content":[{"type":"text","text":"h"}]}]}]},{"type":"tableRow","content":[{"type":"tableCell","attrs":{"colspan":1,"rowspan":1,"colwidth":null,"align":null},"content":[{"type":"paragraph","attrs":{"textAlign":null},"content":[{"type":"text","text":"c1"}]}]},{"type":"tableCell","attrs":{"colspan":1,"rowspan":1,"colwidth":null,"align":null},"content":[{"type":"paragraph","attrs":{"textAlign":null},"content":[{"type":"text","text":"c2"}]}]}]}]},{"type":"paragraph","attrs":{"textAlign":"right"},"content":[{"type":"text","marks":[{"type":"bold"},{"type":"italic"}],"text":"bi"}]}]}
            """;

        // TipTap writes the heading's quotes as plain ", which is the same HTML.
        private const string TipTapHtml = """
            <h1 style="text-align: center;">Title &amp; &quot;q&quot; <strong>x</strong></h1><p>Plain <strong>b</strong> <em>i</em> <u>u</u> <s>s</s> <code>c&lt;</code> <mark>h</mark> <a rel="noopener noreferrer nofollow" href="https://a.com/?x=1&amp;y=2">link</a><br>line2 <span class="rich-text-mention" data-type="mention" data-id="7" data-label="Anna" data-mention-suggestion-char="@">@Anna</span></p><p></p><blockquote><p>q</p></blockquote><pre><code class="language-js">let a = 1 &lt; 2;
            b</code></pre><pre><code>plain</code></pre><ul><li><p>a</p><ul><li><p>n</p></li></ul></li></ul><ol start="3"><li><p>o</p></li></ol><ol><li><p>o1</p></li></ol><ul data-type="taskList"><li data-checked="true" data-type="taskItem"><label><input type="checkbox" checked="checked"><span></span></label><div><p>done</p></div></li><li data-checked="false" data-type="taskItem"><label><input type="checkbox"><span></span></label><div><p>todo</p></div></li></ul><hr><img src="https://x.com/a.png" alt="A" title="T"><table><tbody><tr><th colspan="2"><p>h</p></th></tr><tr><td><p>c1</p></td><td><p>c2</p></td></tr></tbody></table><p style="text-align: right;"><strong><em>bi</em></strong></p>
            """;

        [Fact]
        public void Writes_the_same_html_as_tiptap()
        {
            Assert.Equal(TipTapHtml.ReplaceLineEndings("\n"), RichTextJson.ToHtml(TipTapJson));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Empty_value_is_empty_html(string? json)
        {
            Assert.Equal("", RichTextJson.ToHtml(json));
        }

        [Fact]
        public void Invalid_json_throws()
        {
            Assert.ThrowsAny<JsonException>(() => RichTextJson.ToHtml("{not json"));
        }

        [Fact]
        public void Text_with_shared_marks_shares_their_elements()
        {
            var json = Doc(P(T("a ", new { type = "bold" }), T("b", new { type = "bold" }, new { type = "italic" }), T(" c")));

            Assert.Equal("<p><strong>a <em>b</em></strong> c</p>", RichTextJson.ToHtml(json));
        }

        [Theory]
        [InlineData("javascript:alert(1)")]
        [InlineData("JavaScript:alert(1)")]
        [InlineData("java\tscript:alert(1)")]
        [InlineData(" javascript:alert(1)")]
        [InlineData("data:text/html,<script>alert(1)</script>")]
        [InlineData("vbscript:x")]
        public void Unsafe_links_keep_their_text_only(string href)
        {
            var json = Doc(P(T("click", new { type = "link", attrs = new { href } })));

            Assert.Equal("<p>click</p>", RichTextJson.ToHtml(json));
        }

        [Theory]
        [InlineData("https://tabblazor.com", true)]
        [InlineData("HTTP://tabblazor.com", true)]
        [InlineData("mailto:a@b.se", true)]
        [InlineData("/docs?x=1", true)]
        [InlineData("#top", true)]
        [InlineData("ftp://x", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void Allowed_urls(string? url, bool allowed)
        {
            Assert.Equal(allowed, RichTextJson.IsAllowedUrl(url));
        }

        [Fact]
        public void Images_need_a_safe_url_and_no_mailto()
        {
            var json = Doc(
                new { type = "image", attrs = new { src = "data:image/png;base64,AAAA" } },
                new { type = "image", attrs = new { src = "mailto:a@b.se" } },
                new { type = "image", attrs = new { src = "/a.png", alt = "x\" onerror=\"alert(1)", width = 10 } });

            Assert.Equal("<img src=\"/a.png\" alt=\"x&quot; onerror=&quot;alert(1)\" width=\"10\">", RichTextJson.ToHtml(json));
        }

        [Fact]
        public void Encodes_text_and_attributes_and_keeps_letters()
        {
            var json = Doc(P(T("<script>åäö</script>"),
                new { type = "mention", attrs = new { id = "\"><script>", label = "Björn \"B\"" } }));

            Assert.Equal(
                "<p>&lt;script&gt;åäö&lt;/script&gt;<span class=\"rich-text-mention\" data-type=\"mention\" data-id=\"&quot;&gt;&lt;script&gt;\" " +
                "data-label=\"Björn &quot;B&quot;\" data-mention-suggestion-char=\"@\">@Björn &quot;B&quot;</span></p>",
                RichTextJson.ToHtml(json));
        }

        [Fact]
        public void Ignores_unknown_attribute_values()
        {
            var json = Doc(
                new { type = "paragraph", attrs = new { textAlign = "center; position: fixed" }, content = new[] { T("a") } },
                new { type = "heading", attrs = new { level = 9 }, content = new[] { T("b") } },
                new { type = "orderedList", attrs = new { start = 1, type = "x\"" }, content = Array.Empty<object>() },
                new { type = "codeBlock", attrs = new { language = "js\" onclick=\"x" }, content = new[] { T("c") } });

            Assert.Equal(
                "<p>a</p><h6>b</h6><ol></ol><pre><code class=\"language-js&quot; onclick=&quot;x\">c</code></pre>",
                RichTextJson.ToHtml(json));
        }

        [Fact]
        public void Leaves_out_unknown_nodes_and_marks()
        {
            var json = Doc(
                new { type = "iframe", attrs = new { src = "https://evil" }, content = new[] { P(T("inside")) } },
                P(T("kept", new { type = "fontFamily", attrs = new { fontFamily = "x" } })),
                "not a node");

            Assert.Equal("<p>kept</p>", RichTextJson.ToHtml(json));
        }

        [Fact]
        public void Custom_nodes_use_the_app_renderer()
        {
            var options = new RichTextJsonOptions();
            options.Nodes["callout"] = node =>
                $"<div class=\"callout callout-{RichTextJson.Encode(node.Attr("kind"))}\">{node.ContentHtml}</div>";
            var json = Doc(new { type = "callout", attrs = new { kind = "warning" }, content = new[] { P(T("Careful", new { type = "bold" })) } });

            Assert.Equal("<div class=\"callout callout-warning\"><p><strong>Careful</strong></p></div>", RichTextJson.ToHtml(json, options));
        }

        [Fact]
        public void Table_cells_keep_spans_and_alignment()
        {
            var cell = new { type = "tableCell", attrs = new { colspan = 1, rowspan = 2, align = "center" }, content = new[] { P(T("x")) } };
            var json = Doc(new { type = "table", content = new[] { new { type = "tableRow", content = new[] { cell } } } });

            Assert.Equal("<table><tbody><tr><td rowspan=\"2\" style=\"text-align: center\"><p>x</p></td></tr></tbody></table>",
                RichTextJson.ToHtml(json));
        }
    }
}
