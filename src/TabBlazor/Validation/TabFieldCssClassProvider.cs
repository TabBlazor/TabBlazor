using System.Linq;
using Microsoft.AspNetCore.Components.Forms;

namespace TabBlazor;

public class TabFieldCssClassProvider : FieldCssClassProvider
{
    public override string GetFieldCssClass(EditContext editContext, in FieldIdentifier fieldIdentifier)
    {
        var isValid = !editContext.GetValidationMessages(fieldIdentifier).Any();

        return isValid ? "is-valid" : "is-invalid";
    }
}