using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace TabBlazor;

public interface IFormValidator
{
    Type Component { get; }
    Task<bool> ValidateAsync(object validatorInstance, EditContext editContext);
    bool Validate(object validatorInstance, EditContext editContext);
}