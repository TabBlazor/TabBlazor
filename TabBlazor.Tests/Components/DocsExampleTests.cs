using System.Linq;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Tabler.Docs.Components;
using Tabler.Docs.Components.Buttons;
using Tabler.Docs.Services;

namespace TabBlazor.Tests.Components
{
    public class DocsExampleTests : TabBlazorTestContext
    {
        public DocsExampleTests()
        {
            Services.AddSingleton<ICodeSnippetService, FakeSnippetService>();
        }

        private static RenderFragment Snippet(string title) => builder =>
        {
            builder.OpenComponent<CodeSnippet>(0);
            builder.AddAttribute(1, nameof(CodeSnippet.Title), title);
            builder.AddAttribute(2, nameof(CodeSnippet.Component), typeof(ButtonsBasic));
            builder.AddAttribute(3, nameof(CodeSnippet.ShowExample), false);
            builder.CloseComponent();
        };

        private IRenderedComponent<DocsExample> RenderDocsExample(string relativeUrl, params string[] titles)
        {
            Services.GetRequiredService<NavigationManager>().NavigateTo(relativeUrl);

            return Render<DocsExample>(p => p
                .Add(e => e.Title, "Buttons")
                .AddChildContent(builder =>
                {
                    foreach (var title in titles)
                    {
                        Snippet(title)(builder);
                    }
                }));
        }

        [Fact]
        public void Gives_each_example_a_slug_id_from_its_title()
        {
            var cut = RenderDocsExample("docs/buttons", "Basic Setup", "Button List");

            var ids = cut.FindAll("h4.docs-heading").Select(h => h.Id).ToArray();
            Assert.Equal(new[] { "basic-setup", "button-list" }, ids);
        }

        [Fact]
        public void Renders_an_anchor_link_to_each_example()
        {
            var cut = RenderDocsExample("docs/buttons", "Colors");

            var anchor = cut.Find("h4.docs-heading a.docs-anchor");
            Assert.Equal("docs/buttons#colors", anchor.GetAttribute("href"));

            // Out of the accessibility tree and out of the tab order, so that the heading is announced as
            // "Colors" rather than as the title plus the link. The "On this page" list links here accessibly.
            Assert.Equal("true", anchor.GetAttribute("aria-hidden"));
            Assert.Equal("-1", anchor.GetAttribute("tabindex"));
            Assert.Null(anchor.GetAttribute("aria-label"));
        }

        [Fact]
        public void Links_the_on_this_page_entries_to_the_examples()
        {
            var cut = RenderDocsExample("docs/buttons", "Basic Setup", "Colors");

            var hrefs = cut.FindAll("li.toc-entry a").Select(a => a.GetAttribute("href")).ToArray();
            Assert.Equal(new[] { "docs/buttons#basic-setup", "docs/buttons#colors" }, hrefs);
        }

        [Fact]
        public void Makes_anchors_unique_when_titles_repeat()
        {
            var cut = RenderDocsExample("docs/buttons", "Sizes", "Sizes", "Sizes");

            var ids = cut.FindAll("h4.docs-heading").Select(h => h.Id).ToArray();
            Assert.Equal(new[] { "sizes", "sizes-2", "sizes-3" }, ids);
        }

        [Fact]
        public void Slugifies_punctuation_and_diacritics()
        {
            var cut = RenderDocsExample("docs/buttons", "Grouping & Sortering (på svenska)");

            Assert.Equal("grouping-sortering-pa-svenska", cut.Find("h4.docs-heading").Id);
        }

        [Fact]
        public void Scrolls_to_the_example_when_the_url_has_a_fragment()
        {
            var cut = RenderDocsExample("docs/buttons#colors", "Basic Setup", "Colors");

            var invocation = Assert.Single(JSInterop.Invocations["tabBlazor.scrollToFragment"]);
            Assert.Equal("colors", invocation.Arguments[0]);
            Assert.Equal(false, invocation.Arguments[1]);   // not smooth: the page is still settling
        }

        [Fact]
        public void Does_not_scroll_for_a_fragment_that_matches_no_example()
        {
            RenderDocsExample("docs/buttons#nope", "Basic Setup");

            Assert.Empty(JSInterop.Invocations["tabBlazor.scrollToFragment"]);
        }
    }
}
