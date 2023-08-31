using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;

namespace TabBlazor;

public class TablerDataAnnotationsValidator : IFormValidator
{
    public Type Component => typeof(DataAnnotationsValidator);

    public Task<bool> ValidateAsync(object validatorInstance, EditContext editContext)
    {
        return Task.FromResult(editContext.Validate());
    }

    public bool Validate(object validatorInstance, EditContext editContext)
    {
        return editContext.Validate();
    }
}