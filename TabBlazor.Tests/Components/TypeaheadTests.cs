using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace TabBlazor.Tests.Components
{
    public class TypeaheadTests : TabBlazorTestContext
    {
        private static readonly string[] Customers = { "Alpha Corp", "Beta Ltd" };

        private static Task<IEnumerable<string>> Search(string text) =>
            Task.FromResult(Customers.Where(c => c.Contains(text, StringComparison.OrdinalIgnoreCase)));

        private IRenderedComponent<Typeahead<string, string>> RenderTypeahead(Action<string> onChanged = null)
        {
            return Render<Typeahead<string, string>>(p => p
                .Add(t => t.SearchMethod, Search)
                .Add(t => t.MinimumLength, 1)
                .Add(t => t.Debounce, 1)
                .Add(t => t.SelectedTextExpression, v => v)
                .Add(t => t.SelectedValueChanged, onChanged ?? (_ => { }))
                .Add(t => t.ListTemplate, item => b => b.AddContent(0, item)));
        }

        private static void TypeAndWaitForResults(IRenderedComponent<Typeahead<string, string>> cut, string text)
        {
            var input = cut.Find("input");
            input.Focus();
            input.Input(text);
            cut.WaitForAssertion(() => Assert.Single(cut.FindAll("a.dropdown-item")));
        }

        [Fact]
        public void Renders_search_input_when_nothing_selected()
        {
            var cut = RenderTypeahead();
            Assert.Single(cut.FindAll("input.form-control"));
        }

        [Fact]
        public void Shows_results_after_typing()
        {
            var cut = RenderTypeahead();
            TypeAndWaitForResults(cut, "alp");
            Assert.Equal("Alpha Corp", cut.Find("a.dropdown-item").TextContent.Trim());
        }

        [Fact]
        public void Keyboard_selection_shows_selected_value_immediately()
        {
            string selected = null;
            var cut = RenderTypeahead(v => selected = v);
            TypeAndWaitForResults(cut, "alp");

            cut.Find("input").KeyUp(new KeyboardEventArgs { Key = "ArrowDown" });
            cut.Find("input").KeyUp(new KeyboardEventArgs { Key = "Enter" });

            cut.WaitForAssertion(() =>
            {
                Assert.Equal("Alpha Corp", cut.Find("span.form-control").TextContent.Trim());
                Assert.Empty(cut.FindAll("input"));
            });
            Assert.Equal("Alpha Corp", selected);
        }

        [Fact]
        public void Mouse_selection_shows_selected_value_immediately()
        {
            string selected = null;
            var cut = RenderTypeahead(v => selected = v);
            TypeAndWaitForResults(cut, "bet");

            cut.Find("a.dropdown-item").Click();

            cut.WaitForAssertion(() => Assert.Equal("Beta Ltd", cut.Find("span.form-control").TextContent.Trim()));
            Assert.Equal("Beta Ltd", selected);
        }

        [Fact]
        public void Clicking_selected_value_returns_to_search_input()
        {
            var cut = RenderTypeahead();
            TypeAndWaitForResults(cut, "alp");
            cut.Find("a.dropdown-item").Click();
            cut.WaitForAssertion(() => cut.Find("span.form-control"));

            cut.Find("span.form-control").Click();

            Assert.Single(cut.FindAll("input.form-control"));
        }
    }
}
