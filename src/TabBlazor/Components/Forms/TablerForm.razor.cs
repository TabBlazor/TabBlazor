﻿using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace TabBlazor
{
    public partial class TablerForm : ComponentBase
    {
        [Inject] protected IServiceProvider Provider { get; set; }

        [Parameter(CaptureUnmatchedValues = true)]
        public IDictionary<string, object> UnknownParameters { get; set; }

        [Parameter] public object Model { get; set; }
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public EventCallback<bool> IsValidChanged { get; set; }
        [Parameter] public IFormValidator Validator { get; set; }
        [Parameter] public EventCallback<EditContext> OnValidSubmit { get; set; }
        [Parameter] public bool IsValid { get; set; }
        public DynamicComponent ValidatorInstance { get; set; }

        public bool IsModified => true;
        protected EditContext EditContext { get; set; }
        public bool RenderForm { get; set; }
        public bool CanSubmit => IsValid && IsModified;

        private bool initialized;

        protected override async Task OnParametersSetAsync()
        {
            await SetupFormAsync();
        }

        protected override void OnInitialized()
        {
        }

        private async Task SetupFormAsync()
        {
            if (Model == null)
            {
                RenderForm = false;
                EditContext = null;
                return;
            }

            Validator = GetValidator();
            
            if (EditContext == null || !EditContext.Model.Equals(Model))
            {
                EditContext = new EditContext(Model);
                await ValidateAsync();
            }

            EditContext.SetFieldCssClassProvider(new TabFieldCssClassProvider());

            RenderForm = true;
        }
        
        private IFormValidator GetValidator() => Validator ?? Provider.GetRequiredService<IFormValidator>();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (RenderForm)
            {
                IsValid = await ValidateAsync();
                OnAfterModelValidation(IsValid);
            }
        }

        public void OnAfterModelValidation(bool isValid)
        {
            if (isValid != IsValid || !initialized)
            {
                initialized = true;
                IsValid = isValid;
                StateHasChanged();
                IsValidChanged.InvokeAsync(IsValid);
            }
        }

        public async Task<bool> ValidateAsync()
        {
            IsValid = await Validator.ValidateAsync(ValidatorInstance?.Instance, EditContext);
            OnAfterModelValidation(IsValid);

            return IsValid;
        }

        public bool Validate()
        {
            Validator.Validate(ValidatorInstance.Instance, EditContext);
            OnAfterModelValidation(IsValid);

            return IsValid;
        }

        protected async Task HandleValidSubmit()
        {
            if (CanSubmit)
            {
                await OnValidSubmit.InvokeAsync(EditContext);
                EditContext?.MarkAsUnmodified();
            }
        }

        public void Dispose()
        {
            
            EditContext = null;
        }

        protected string GetSaveButtonClass()
        {
            return "";
        }
    }
}