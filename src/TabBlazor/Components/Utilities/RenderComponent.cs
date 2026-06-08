using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace TabBlazor
{
    /// <summary>
    /// A fluent builder describing <typeparamref name="TComponent"/> and its parameters, used to pass a component
    /// into APIs like <see cref="Services.IModalService.ShowAsync{TComponent}"/>. Chain <see cref="Set"/> calls to
    /// supply parameters, e.g. <c>new RenderComponent&lt;MyComp&gt;().Set(c => c.Value, x)</c>.
    /// </summary>
    public class RenderComponent<TComponent> where TComponent : IComponent
    {
        private static readonly Type TComponentType = typeof(TComponent);
        private readonly Dictionary<string, object> parameters = new(StringComparer.Ordinal);

        /// <summary>Sets a <c>[Parameter]</c> (or <c>[CascadingParameter]</c>) on the component and returns this builder for chaining.</summary>
        /// <param name="parameterSelector">Selects the target parameter property, e.g. <c>c => c.Title</c>.</param>
        /// <param name="value">The value to assign. Must not be null.</param>
        public RenderComponent<TComponent> Set<TValue>(Expression<Func<TComponent, TValue>> parameterSelector, TValue value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            parameters.Add(GetParameterName(parameterSelector), value);
            return this;
        }

        private static string GetParameterName<TValue>(Expression<Func<TComponent, TValue>> parameterSelector)
        {
            if (parameterSelector is null)
                throw new ArgumentNullException(nameof(parameterSelector));

            if (parameterSelector.Body is not MemberExpression memberExpression ||
                memberExpression.Member is not PropertyInfo propInfoCandidate)
                throw new ArgumentException($"The parameter selector '{parameterSelector}' does not resolve to a public property on the component '{typeof(TComponent)}'.", nameof(parameterSelector));

            var propertyInfo = propInfoCandidate.DeclaringType != TComponentType
                ? TComponentType.GetProperty(propInfoCandidate.Name, propInfoCandidate.PropertyType)
                : propInfoCandidate;

            var paramAttr = propertyInfo?.GetCustomAttribute<ParameterAttribute>(inherit: true);
            var cascadingParameterAttribute = propertyInfo?.GetCustomAttribute<CascadingParameterAttribute>();


            if (propertyInfo is null || (paramAttr is null && cascadingParameterAttribute is null))
                throw new ArgumentException($"The parameter selector '{parameterSelector}' does not resolve to a public property on the component '{typeof(TComponent)}' with a [Parameter] or [CascadingParameter] attribute.", nameof(parameterSelector));

            return propertyInfo.Name;
        }

        /// <summary>The render fragment that instantiates the component with the configured parameters.</summary>
        public RenderFragment Contents
        {
            get
            {
                RenderFragment content = new(x =>
                {
                    int seq = 1;
                    x.OpenComponent(seq++, TComponentType);
                    if (parameters != null)
                    {
                        foreach (var parameter in parameters)
                            x.AddAttribute(seq++, parameter.Key, parameter.Value);
                    }

                    x.CloseComponent();
                });
                return content;
            }
        }


    }
}
