using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabBlazor
{
    public class RenderComponent
    {
        public RenderComponent(Type componentType, Dictionary<string,object> parameters = null)
        {
            if (!typeof(ComponentBase).IsAssignableFrom(componentType))
            {
                throw new ArgumentException($"{componentType.FullName} must be a Blazor Component");
            }

            ComponentType = componentType;
            Parameters = parameters;
        }

        public Type ComponentType { get; set; }
        public Dictionary<string, object> Parameters { get; }

        public RenderFragment Contents
        {
            get
            {
                RenderFragment content = new RenderFragment(x =>
                {
                    int seq = 1;
                    x.OpenComponent(seq++, ComponentType);
                    if (Parameters != null)
                    {
                        foreach (var parameter in Parameters)
                            x.AddAttribute(seq++, parameter.Key, parameter.Value);
                    }

                    x.CloseComponent();
                });
                return content;
            }
        }
    }
}
