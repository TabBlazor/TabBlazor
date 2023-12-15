using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Options;
using TabBlazor.Components.Offcanvas;

namespace TabBlazor;

public partial class OffcanvasContainer
{
    [Inject] private IOffcanvasService offcanvasService { get; set; }

    protected override void OnInitialized()
    {
        offcanvasService.OnChanged += StateHasChanged99;

        base.OnInitialized();
    }

    private void OnClickOutside(OffcanvasModel model)
    {
        if (model.Options.CloseOnClickOutside)
        {
            offcanvasService.Close();
        }
    }
    
    protected void OnKeyDown(KeyboardEventArgs e, OffcanvasModel offcanvasModel)
    {
        if (e.Key == "Escape" && offcanvasModel.Options.CloseOnEsc)
        {
            offcanvasService.Close();
        }
    }

    private void StateHasChanged99()
    {
        StateHasChanged();
    }

    private string GetClasses(OffcanvasModel offcanvasModel) => new ClassBuilder()
        .Add("offcanvas")
        .Add($"offcanvas-{offcanvasModel.Options.Position.ToString().ToLower()}")
        .Add(offcanvasModel.Options.WrapperCssClass)
        .Add("show")
        .ToString();
}