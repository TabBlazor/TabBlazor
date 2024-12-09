namespace TabBlazor
{
    public partial class ContentViewer : TablerBaseComponent, IAsyncDisposable
    {
        [Inject] Services.TablerService TablerService { get; set; }
        [Parameter] public string ContentType { get; set; }
        [Parameter] public byte[] Content { get; set; }
        [Parameter] public string UrlSuffix { get; set; }

        private string objectURL;


        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (Content != null && objectURL == null)
            {
                objectURL = await TablerService.CreateObjectURLAsync(ContentType, Content);
                StateHasChanged();
            }
        }


        public async ValueTask DisposeAsync()
        {
            if (objectURL != null) {
                await TablerService.RevokeObjectURLAsync(objectURL);
            }    
        }

    }
}