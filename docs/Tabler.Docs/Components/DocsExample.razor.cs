using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TabBlazor;
using TabBlazor.Services;
using Tabler.Docs.Components.TypeBrowsers;

namespace Tabler.Docs.Components
{
    public partial class DocsExample : ComponentBase, IDisposable
    {
        [Inject] public TablerService TablerService { get; set; }
        [Inject] private IModalService modalService { get; set; }
        [Inject] private NavigationManager navigationManager { get; set; }

        [Parameter] public string Title { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public RenderFragment Description { get; set; }
        [Parameter] public Type ComponentType { get; set; }

        public List<CodeSnippet> CodeSnippets = new List<CodeSnippet>();

        private string pendingFragment;

        protected override void OnInitialized()
        {
            pendingFragment = GetFragment(navigationManager.Uri);
            navigationManager.LocationChanged += OnLocationChanged;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (string.IsNullOrEmpty(pendingFragment))
            {
                return;
            }

            // Wait until the snippet owning the fragment has registered, and until every example on
            // the page has loaded its code listing - scrolling earlier lands short, because the
            // examples above the target still grow as their listings arrive.
            if (!CodeSnippets.Any(snippet => string.Equals(snippet.Anchor, pendingFragment, StringComparison.OrdinalIgnoreCase))
                || !CodeSnippets.All(snippet => snippet.Loaded))
            {
                return;
            }

            var fragment = pendingFragment;
            pendingFragment = null;

            // Not smooth: on load the examples are still settling, and a smooth scroll gets cancelled.
            await TablerService.ScrollToFragment(fragment, smooth: false);
        }

        private void OnLocationChanged(object sender, LocationChangedEventArgs e)
        {
            var fragment = GetFragment(e.Location);
            if (string.IsNullOrEmpty(fragment))
            {
                return;
            }

            pendingFragment = fragment;
            StateHasChanged();
        }

        /// <summary>
        /// Scrolls to an example. Used by the in-page links, which Blazor turns into a
        /// push-state navigation that scrolls neither natively nor through <see cref="OnLocationChanged"/>.
        /// </summary>
        public async Task ScrollToAnchor(string anchor)
        {
            pendingFragment = null;

            // Not smooth: Blazor scrolls on the same click, which cancels an animated scroll midway.
            await TablerService.ScrollToFragment(anchor, smooth: false);
        }

        /// <summary>
        /// Link to a single example, e.g. <c>docs/buttons#colors</c>.
        /// </summary>
        public string AnchorHref(CodeSnippet codeSnippet)
        {
            var path = navigationManager.ToBaseRelativePath(new Uri(navigationManager.Uri).GetLeftPart(UriPartial.Path));
            return $"{path}#{codeSnippet.Anchor}";
        }

        /// <summary>
        /// Called by a <see cref="CodeSnippet"/> once its code listing has loaded, so that a pending
        /// deep link is re-evaluated against the settled page.
        /// </summary>
        internal void NotifySnippetLoaded()
        {
            if (!string.IsNullOrEmpty(pendingFragment))
            {
                StateHasChanged();
            }
        }

        public void AddCodeSnippet(CodeSnippet codeSnippet)
        {
            codeSnippet.Anchor = CreateUniqueAnchor(codeSnippet.Title);
            CodeSnippets.Add(codeSnippet);
            StateHasChanged();
        }

        public void RemoveCodeSnippet(CodeSnippet codeSnippet)
        {
            CodeSnippets.Remove(codeSnippet);
            StateHasChanged();
        }

        private async Task OpenComponentModal()
        {
            if (ComponentType != null)
            {
                var component = new RenderComponent<TypeBrowser>().Set(e => e.Type, ComponentType);
                var result = await modalService.ShowAsync("Component API", component, new ModalOptions { Size = ModalSize.Large });

            }
        }

        private string CreateUniqueAnchor(string title)
        {
            var baseAnchor = Slugify(title);
            var anchor = baseAnchor;
            var suffix = 2;

            while (CodeSnippets.Any(snippet => string.Equals(snippet.Anchor, anchor, StringComparison.OrdinalIgnoreCase)))
            {
                anchor = $"{baseAnchor}-{suffix++}";
            }

            return anchor;
        }

        private static string Slugify(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "example";
            }

            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);

            foreach (var character in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(char.ToLowerInvariant(character));
                }
                else if (builder.Length > 0 && builder[builder.Length - 1] != '-')
                {
                    builder.Append('-');
                }
            }

            var slug = builder.ToString().Trim('-');
            return string.IsNullOrEmpty(slug) ? "example" : slug;
        }

        private static string GetFragment(string uri)
        {
            var index = uri.IndexOf('#');
            return index < 0 ? null : Uri.UnescapeDataString(uri.Substring(index + 1));
        }

        public void Dispose()
        {
            navigationManager.LocationChanged -= OnLocationChanged;
        }
    }
}
