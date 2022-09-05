using Microsoft.AspNetCore.Components.Forms;

namespace TabBlazor;

public interface IFormValidator
{
    void EnableValidation(EditContext editContext);
}