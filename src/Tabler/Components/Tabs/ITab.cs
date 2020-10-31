using Microsoft.AspNetCore.Components;

namespace Tabler.Components
{
    public interface ITab
    {
        RenderFragment ChildContent { get; }
    }
}
