using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace TabBlazor
{
    public class HtmlElement : ComponentBase
    {
        [Parameter] public string Tag { get; set; }
        [Parameter] public ElementReference ElementRef { get; set; }
        [Parameter] public Action<ElementReference> ElementRefChanged { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }

        [Parameter(CaptureUnmatchedValues = true)]
        public IDictionary<string, object> UnmatchedParameters { get; set; }


        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            base.BuildRenderTree(builder);

            var seq = 0;

            builder.OpenElement(seq++, Tag);
            builder.AddMultipleAttributes(seq++, UnmatchedParameters);
            builder.AddElementReferenceCapture(seq++, reference =>
            {
                ElementRef = reference;
                ElementRefChanged?.Invoke(ElementRef);
            });
            builder.AddContent(seq++, ChildContent);
            builder.CloseElement();
        }
    }
}