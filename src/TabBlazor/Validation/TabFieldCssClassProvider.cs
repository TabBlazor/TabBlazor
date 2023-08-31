using Microsoft.AspNetCore.Components.Forms;

namespace TabBlazor;

public class TabFieldCssClassProvider : FieldCssClassProvider
{
    public override string GetFieldCssClass(EditContext editContext, in FieldIdentifier fieldIdentifier)
    {
        var isValid = !editContext.GetValidationMessages(fieldIdentifier).Any();
        var isModified = editContext.IsModified(fieldIdentifier);
        if (isModified)
        {
            return isValid ? "is-valid" : "is-invalid";
        }

        if (!isValid)
        {
            return "is-invalid";
        }

        return "";
    }
}