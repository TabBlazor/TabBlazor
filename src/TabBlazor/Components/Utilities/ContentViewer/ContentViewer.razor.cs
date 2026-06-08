namespace TabBlazor
{
    /// <summary>
    /// Displays in-memory byte content (e.g. an image or PDF) by creating a browser object URL for it.
    /// </summary>
    public partial class ContentViewer : TablerBaseComponent, IAsyncDisposable
    {
        [Inject] Services.TablerService TablerService { get; set; }
        /// <summary>The MIME type of <see cref="Content"/> (e.g. <c>image/png</c>, <c>application/pdf</c>).</summary>
        [Parameter] public string ContentType { get; set; }
        /// <summary>The raw content bytes to display.</summary>
        [Parameter] public byte[] Content { get; set; }
        /// <summary>Optional suffix appended to the generated object URL.</summary>
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