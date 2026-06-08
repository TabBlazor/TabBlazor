using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Options;

namespace TabBlazor
{
    /// <summary>
    /// Base class for TabBlazor components. Provides common color, content and click parameters,
    /// HTML attribute passthrough, and CSS class composition via <see cref="ClassBuilder"/>.
    /// </summary>
    public abstract class TablerBaseComponent : ComponentBase
    {
        /// <summary>
        /// Text color applied to the component. Defaults to <see cref="TablerColor.Default"/>.
        /// </summary>
        [Parameter] public TablerColor TextColor { get; set; } = TablerColor.Default;

        /// <summary>
        /// Background color applied to the component. Defaults to <see cref="TablerColor.Default"/>.
        /// </summary>
        [Parameter] public TablerColor BackgroundColor { get; set; } = TablerColor.Default;

        /// <summary>
        /// Content rendered inside the component.
        /// </summary>
        [Parameter] public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Invoked when the component is clicked.
        /// </summary>
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

        /// <summary>
        /// Monitors the configured <see cref="TablerOptions"/> for the component.
        /// </summary>
        [Inject] protected IOptionsMonitor<TablerOptions> Options { get; set; }

        /// <summary>
        /// Captures any HTML attributes not matched by a declared parameter so they are forwarded to the rendered element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IDictionary<string, object> UnmatchedParameters { get; set; }

        protected ClassBuilder ClassBuilder => new ClassBuilder(ProvidedCssClasses);
        private string providedCssClasses;
        protected string ProvidedCssClasses
        {
            get
            {
                var cssClasses = GetUnmatchedParameter("class")?.ToString();

                if (cssClasses != null)
                {
                    providedCssClasses = cssClasses;
                }

                return providedCssClasses;
            }
        }

        protected virtual string ClassNames => ClassBuilder.ToString();

        protected object GetUnmatchedParameter(string key)
        {
            if (UnmatchedParameters?.ContainsKey(key) ?? false)
            {
                var value = UnmatchedParameters[key];
                UnmatchedParameters.Remove(key);
                return value;
            }

            return null;
        }
    }
}