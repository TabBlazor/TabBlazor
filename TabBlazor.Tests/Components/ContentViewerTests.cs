using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace TabBlazor.Tests.Components
{
    public class ContentViewerTests : TabBlazorTestContext
    {
        [Fact]
        public async Task Dispose_revokes_the_object_url()
        {
            JSInterop.Setup<string>("tabBlazor.createObjectURL", _ => true).SetResult("blob:pdf");
            JSInterop.SetupVoid("tabBlazor.revokeObjectURL", _ => true).SetVoidResult();
            var cut = Render<ContentViewer>(p => p.Add(c => c.ContentType, "application/pdf").Add(c => c.Content, [1, 2, 3]));

            await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());

            var revoke = Assert.Single(JSInterop.Invocations, i => i.Identifier == "tabBlazor.revokeObjectURL");
            Assert.Equal("blob:pdf", revoke.Arguments[0]);
        }

        [Fact]
        public async Task Dispose_after_the_circuit_disconnected_does_not_throw()
        {
            JSInterop.Setup<string>("tabBlazor.createObjectURL", _ => true).SetResult("blob:pdf");
            JSInterop.SetupVoid("tabBlazor.revokeObjectURL", _ => true).SetException(new JSDisconnectedException("The circuit is gone."));
            var cut = Render<ContentViewer>(p => p.Add(c => c.ContentType, "application/pdf").Add(c => c.Content, [1, 2, 3]));

            var exception = await Record.ExceptionAsync(() => cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask()));

            Assert.Null(exception);
        }
    }
}
