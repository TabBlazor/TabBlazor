using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace TabBlazor;

public class TabValidationMessage<TValue> : ValidationMessage<TValue>
{
    [Parameter] public string CssClass { get; set; } = "invalid-feedback";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(2, "class", CssClass);
        builder.OpenRegion(1);
        base.BuildRenderTree(builder);
        builder.CloseRegion();
        builder.CloseElement();
    }
}