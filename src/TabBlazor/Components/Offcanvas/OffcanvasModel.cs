
namespace TabBlazor.Components.Offcanvas
{
    public class OffcanvasModel
    {
        internal TaskCompletionSource<OffcanvasResult> TaskSource { get; } = new();
        public Task<OffcanvasResult> Task { get { return TaskSource.Task; } }
        public string Title { get; set; }
        public RenderFragment Contents { get; set; }

        public OffcanvasOptions Options { get; set; } = new();

    }
}
