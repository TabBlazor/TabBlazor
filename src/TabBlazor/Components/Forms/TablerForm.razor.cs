using Microsoft.AspNetCore.Components;
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

        public bool IsModified => true;
        protected EditContext EditContext { get; set; }
        public bool RenderForm { get; set; }
        public bool CanSubmit => IsValid && IsModified;

        private bool initialized;

        protected override void OnParametersSet()
        {
            SetupForm();
        }

        protected override void OnInitialized()
        {
        }

        private void SetupForm()
        {
            if (Model == null)
            {
                RenderForm = false;
                EditContext = null;
                return;
            }

            if (EditContext == null || !EditContext.Model.Equals(Model))
            {
                EditContext = new EditContext(Model);
                Validator = GetValidator();
                Validator.EnableValidation(EditContext);
                EditContext.Validate();
            }
            
            EditContext.SetFieldCssClassProvider(new TabFieldCssClassProvider());

            RenderForm = true;
        }
        
        private IFormValidator GetValidator() => Validator ?? Provider.GetRequiredService<IFormValidator>();

        protected override void OnAfterRender(bool firstRender)
        {
            if (RenderForm)
            {
                IsValid = EditContext.Validate();
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

        public void Validate()
        {
            IsValid = EditContext.Validate();
            OnAfterModelValidation(IsValid);
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
            //return new CssBuilder()
            //    .AddClass("btn btn-primary")
            //    .AddClass("disabled text-muted", !CanSubmit)
            //    .Build();
        }
    }
}