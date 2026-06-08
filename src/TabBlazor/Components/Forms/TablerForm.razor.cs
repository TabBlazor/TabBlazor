using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace TabBlazor
{
    /// <summary>
    /// Form container that wraps a model in an <see cref="EditContext"/> and runs validation through a pluggable <see cref="IFormValidator"/>.
    /// Place inputs and a submit control inside it to build a validated form.
    /// </summary>
    /// <remarks>
    /// When <see cref="Model"/> is null nothing is rendered except the child content; the form activates once a model is supplied.
    /// Uses the DI-registered <see cref="IFormValidator"/> (DataAnnotations by default) unless <see cref="Validator"/> is set explicitly.
    /// </remarks>
    public partial class TablerForm : ComponentBase
    {
        [Inject] protected IServiceProvider Provider { get; set; }

        /// <summary>
        /// Captures any unmatched HTML attributes and forwards them to the underlying form element.
        /// </summary>
        [Parameter(CaptureUnmatchedValues = true)]
        public IDictionary<string, object> UnknownParameters { get; set; }

        /// <summary>
        /// The model object the form edits and validates. The form renders only when this is non-null.
        /// </summary>
        [Parameter] public object Model { get; set; }

        /// <summary>
        /// The form content, typically input components and a submit button.
        /// </summary>
        [Parameter] public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// The validator used to validate the model. Defaults to the <see cref="IFormValidator"/> registered in DI when not set.
        /// </summary>
        [Parameter] public IFormValidator Validator { get; set; }

        /// <summary>
        /// Invoked when the form is submitted and the model is valid; receives the current <see cref="EditContext"/>.
        /// </summary>
        [Parameter] public EventCallback<EditContext> OnValidSubmit { get; set; }

        /// <summary>
        /// Whether the model currently passes validation. Bind with <see cref="IsValidChanged"/> to observe validity.
        /// </summary>
        [Parameter] public bool IsValid { get; set; }

        /// <summary>
        /// Raised when <see cref="IsValid"/> changes, enabling two-way binding of the validity state.
        /// </summary>
        [Parameter] public EventCallback<bool> IsValidChanged { get; set; }

        /// <summary>
        /// The dynamically instantiated validator component instance used during validation.
        /// </summary>
        public DynamicComponent ValidatorInstance { get; set; }

        /// <summary>
        /// Indicates whether the model has been modified. Always returns <c>true</c>.
        /// </summary>
        public bool IsModified => true;
        protected EditContext EditContext { get; set; }

        /// <summary>
        /// Whether the form (and its <see cref="EditContext"/>) is currently rendered; <c>true</c> only when a model is set.
        /// </summary>
        public bool RenderForm { get; set; }

        /// <summary>
        /// Whether submission is allowed, i.e. the model is both valid and modified.
        /// </summary>
        public bool CanSubmit => IsValid && IsModified;

        private bool initialized;

        protected override async Task OnParametersSetAsync()
        {
            await SetupFormAsync();
        }
        
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(DataAnnotationsValidator))] // Prevent HeadOutlet from being
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

            RenderForm = true;
            Validator = GetValidator();
            
            if (EditContext == null || !EditContext.Model.Equals(Model))
            {
                EditContext = new EditContext(Model);
                await ValidateAsync();
            }

            EditContext.SetFieldCssClassProvider(new TabFieldCssClassProvider());
        }
        
        private IFormValidator GetValidator() => Validator ?? Provider.GetRequiredService<IFormValidator>();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (RenderForm)
            {
                var valid = await ValidateAsync();
                OnAfterModelValidation(valid);
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
            var valid  = await Validator.ValidateAsync(ValidatorInstance?.Instance, EditContext);
            OnAfterModelValidation(valid);

            return IsValid;
        }

        public bool Validate()
        {
            var valid = Validator.Validate(ValidatorInstance.Instance, EditContext);
            OnAfterModelValidation(valid);

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