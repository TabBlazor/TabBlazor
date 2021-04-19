using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace TabBlazor
{
    public class RenderComponent<TComponent> where TComponent : IComponent
    {
        private static readonly Type TComponentType = typeof(TComponent);
        private readonly Dictionary<string, object> parameters = new(StringComparer.Ordinal);

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

            if (propertyInfo is null || paramAttr is null)
                throw new ArgumentException($"The parameter selector '{parameterSelector}' does not resolve to a public property on the component '{typeof(TComponent)}' with a [Parameter] or [CascadingParameter] attribute.", nameof(parameterSelector));

            return propertyInfo.Name;
        }

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
