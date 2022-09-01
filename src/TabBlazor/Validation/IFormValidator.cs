using Microsoft.AspNetCore.Components.Forms;

public interface IFormValidator
{
    void EnableValidation(EditContext editContext);
}