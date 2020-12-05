using Microsoft.AspNetCore.Components;

namespace TabBlazor.Components
{
    public interface ITab
    {
        RenderFragment ChildContent { get; }
    }
}
