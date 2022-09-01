using Microsoft.AspNetCore.Components.Forms;

public class TablerDataAnnotationsValidator : IFormValidator
{
    public void EnableValidation(EditContext editContext) => editContext.EnableDataAnnotationsValidation();
}