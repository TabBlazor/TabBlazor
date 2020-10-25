using Microsoft.AspNetCore.Components;

namespace Tabler.Components
{
    public interface ITablerTab
    {
        RenderFragment ChildContent { get; }
    }
}
