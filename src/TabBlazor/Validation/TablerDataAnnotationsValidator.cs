using Microsoft.AspNetCore.Components.Forms;

namespace TabBlazor;

public class TablerDataAnnotationsValidator : IFormValidator
{
    public void EnableValidation(EditContext editContext) => editContext.EnableDataAnnotationsValidation();
}