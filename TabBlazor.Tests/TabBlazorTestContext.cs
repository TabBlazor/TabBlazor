using Microsoft.Extensions.DependencyInjection;

namespace TabBlazor.Tests
{
    public abstract class TabBlazorTestContext : BunitContext
    {
        protected TabBlazorTestContext()
        {
            Services.AddTabBlazor();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }
    }
}
