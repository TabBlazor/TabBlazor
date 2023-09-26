namespace TabBlazor
{
    public partial class OffcanvasContainer
    {

        [Inject] private IOffcanvasService offcanvasService { get; set; }


        protected override void OnInitialized()
        {
            offcanvasService.OnChanged += StateHasChanged99;

            base.OnInitialized();
        }

        private void StateHasChanged99()
        {
            StateHasChanged();
        }
    }
}