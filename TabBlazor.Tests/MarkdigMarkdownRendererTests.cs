using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace TabBlazor.Tests
{
    public class MarkdigMarkdownRendererTests
    {
        private static string Render(string markdown, Action<MarkdownRendererOptions>? configure = null)
        {
            var options = new MarkdownRendererOptions();
            configure?.Invoke(options);
            return new MarkdigMarkdownRenderer(options).Render(markdown).Value;
        }

        [Fact]
        public void Renders_commonmark_tables_task_lists_and_strikethrough()
        {
            var html = Render("# Title\n\n- [x] done\n\n| a | b |\n|---|---|\n| 1 | ~~2~~ |");

            Assert.Contains("<h1>Title</h1>", html);
            Assert.Contains("type=\"checkbox\"", html);
            Assert.Contains("<table>", html);
            Assert.Contains("<del>2</del>", html);
        }

        [Fact]
        public void Returns_empty_markup_for_empty_input()
        {
            Assert.Equal("", Render(""));
            Assert.Equal("", new MarkdigMarkdownRenderer().Render(null).Value);
        }

        [Fact]
        public void Shows_raw_html_as_text()
        {
            var html = Render("<script>alert(1)</script>\n\n<img src=x onerror=alert(1)>");

            Assert.DoesNotContain("<script", html);
            Assert.DoesNotContain("<img", html);
            Assert.Contains("&lt;script&gt;", html);
        }

        [Theory]
        [InlineData("[x](javascript:alert(1))")]
        [InlineData("[x](JAVASCRIPT:alert(1))")]
        [InlineData("[x](&#106;avascript:alert(1))")]
        [InlineData("[x](vbscript:msgbox)")]
        [InlineData("[x](data:text/html;base64,PHNjcmlwdD4=)")]
        [InlineData("[x][r]\n\n[r]: javascript:alert(1)")]
        public void Neutralizes_links_with_unsafe_schemes(string markdown)
        {
            var html = Render(markdown);

            Assert.Contains("<a href=\"#\">x</a>", html);
        }

        [Fact]
        public void Neutralizes_unsafe_autolinks()
        {
            var html = Render("<javascript:alert(1)>");

            Assert.DoesNotContain("href=\"javascript", html, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Keeps_safe_and_relative_links()
        {
            var html = Render("[a](https://x.test) [b](/docs/a:b) [c](mailto:a@b.test) [d](#top) [e](page.md)");

            Assert.Contains("href=\"https://x.test\"", html);
            Assert.Contains("href=\"/docs/a:b\"", html);
            Assert.Contains("href=\"mailto:a@b.test\"", html);
            Assert.Contains("href=\"#top\"", html);
            Assert.Contains("href=\"page.md\"", html);
        }

        [Fact]
        public void Blocks_data_images_unless_allowed()
        {
            const string markdown = "![i](data:image/png;base64,AAAA)";

            Assert.Contains("src=\"\"", Render(markdown));
            Assert.Contains("src=\"data:image/png;base64,AAAA\"", Render(markdown, o => o.AllowedImageUrlSchemes.Add("data")));
        }

        [Fact]
        public void Does_not_allow_generic_attributes_by_default()
        {
            var html = Render("# Title {onclick=alert(1)}");

            Assert.DoesNotContain("onclick=\"", html);
        }

        [Fact]
        public void AllowHtml_passes_raw_html_through_sanitizer()
        {
            var seen = new List<string>();
            var html = Render("<b>bold</b><script>x</script>", o => o.AllowHtml(input =>
            {
                seen.Add(input);
                return input.Replace("<script>x</script>", "");
            }));

            Assert.Contains("<b>bold</b>", seen[0]);
            Assert.Contains("<script>", seen[0]);
            Assert.DoesNotContain("<script>", html);
        }

        [Fact]
        public void AllowHtml_requires_a_sanitizer()
        {
            Assert.Throws<ArgumentNullException>(() => new MarkdownRendererOptions().AllowHtml(null!));
        }

        [Fact]
        public void Soft_line_breaks_can_render_as_line_breaks()
        {
            Assert.DoesNotContain("<br", Render("a\nb"));
            Assert.Contains("<br", Render("a\nb", o => o.SoftLineBreaksAsHardLineBreaks = true));
        }

        [Fact]
        public void AddTabBlazorMarkdown_registers_renderer()
        {
            var services = new ServiceCollection().AddTabBlazorMarkdown().BuildServiceProvider();

            Assert.IsType<MarkdigMarkdownRenderer>(services.GetRequiredService<IMarkdownRenderer>());
        }
    }
}
