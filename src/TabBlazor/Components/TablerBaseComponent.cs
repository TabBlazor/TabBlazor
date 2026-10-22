using System.Collections.Generic;
using System.Threading.Tasks;
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

        private string providedStyle;
        private bool providedStyleIsCurrent;

        /// <summary>
        /// The <c>style</c> attribute passed to the component, read once per parameter set so it
        /// survives the component re-rendering itself and clears when the parent removes it.
        /// </summary>
        protected string ProvidedStyle
        {
            get
            {
                if (!providedStyleIsCurrent)
                {
                    providedStyle = GetUnmatchedParameter("style")?.ToString();
                    providedStyleIsCurrent = true;
                }

                return providedStyle;
            }
        }

        /// <summary>
        /// The provided <c>style</c> attribute with the CSS custom property <paramref name="name"/> appended,
        /// or the provided style alone when <paramref name="value"/> is empty.
        /// </summary>
        protected string AppendCssVariable(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ProvidedStyle;
            }

            var style = ProvidedStyle?.Trim().TrimEnd(';');
            return string.IsNullOrEmpty(style) ? $"{name}: {value};" : $"{style}; {name}: {value};";
        }

        public override Task SetParametersAsync(ParameterView parameters)
        {
            providedStyleIsCurrent = false;
            return base.SetParametersAsync(parameters);
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