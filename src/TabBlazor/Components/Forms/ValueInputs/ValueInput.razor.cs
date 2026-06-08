using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Globalization;
using System.Linq.Expressions;

namespace TabBlazor
{
    /// <summary>
    /// A generic text-backed input that parses and binds to a strongly typed <typeparamref name="TValue"/>,
    /// integrating with an ambient <see cref="EditContext"/> for validation. Reports a parse failure as a
    /// validation message instead of throwing.
    /// </summary>
    public partial class ValueInput<TValue> : TablerBaseComponent, IDisposable
    {
        private string currentValue;
        private FieldIdentifier? fieldIdentifier;
        private string FieldCssClasses;
        private ValidationMessageStore validationMessageStore;

        /// <summary>The bound value. Supports two-way binding via <c>@bind-Value</c>.</summary>
        [Parameter] public TValue Value { get; set; }
        /// <summary>Raised when a successfully parsed value changes.</summary>
        [Parameter] public EventCallback<TValue> ValueChanged { get; set; }
        /// <summary>Identifies the bound field for validation; set automatically by <c>@bind-Value</c>.</summary>
        [Parameter] public Expression<Func<TValue>> ValueExpression { get; set; }
        /// <summary>The label text shown for the input.</summary>
        [Parameter] public string Label { get; set; }
        /// <summary>Controls the visual layout (e.g. flush). Defaults to <see cref="DisplayMode.Default"/>.</summary>
        [Parameter] public DisplayMode DisplayMode { get; set; } = DisplayMode.Default;
        /// <summary>Validation message shown when the entered text cannot be parsed to <typeparamref name="TValue"/>.</summary>
        [Parameter] public string ParsingErrorMessage { get; set; }

        [CascadingParameter] private EditContext CascadedEditContext { get; set; }

        /// <summary>True when the current input parsed successfully to <typeparamref name="TValue"/>.</summary>
        public bool IsValid { get; set; } = true;

        private string CurrentValue
        {
            get => currentValue;

            set
            {
                currentValue = value;
                TValue newValue;
                if (typeof(TValue) == typeof(Guid))
                {
                    IsValid = Guid.TryParse(value, out var guidValue);
                    newValue = (TValue)(object)guidValue;
                }
                else
                {
                    //https://github.com/dotnet/aspnetcore/blob/52eff90fbcfca39b7eb58baad597df6a99a542b0/src/Components/Web/src/Forms/InputExtensions.cs
                    IsValid = BindConverter.TryConvertTo<TValue>(value, CultureInfo.CurrentCulture, out newValue);
                }

                if (IsValid)
                {
                    ValueChanged.InvokeAsync(newValue);
                }

                UpdateValidationMessages();
            }
        }

        private void UpdateValidationMessages()
        {
            if (fieldIdentifier is not FieldIdentifier fid) { return; }
            if (validationMessageStore == null) { return; }

            validationMessageStore.Clear(fid);

            if (!IsValid)
            {
                var message = ParsingErrorMessage ?? $"The {fid.FieldName} field is not valid.";
                validationMessageStore.Add(fid, message);
            }

            CascadedEditContext?.NotifyFieldChanged(fid);
        }

        protected override void OnInitialized()
        {
            if (ValueExpression != null)
            {
                fieldIdentifier = FieldIdentifier.Create(ValueExpression);
            }

            if (CascadedEditContext != null)
            {
                validationMessageStore = new ValidationMessageStore(CascadedEditContext);
                CascadedEditContext.OnValidationStateChanged += SetValidationClasses;
            }
        }

        protected override void OnParametersSet()
        {
            currentValue = Value?.ToString();
            base.OnParametersSet();
        }

        private void SetValidationClasses(object sender, ValidationStateChangedEventArgs args)
        {
            if (fieldIdentifier is not FieldIdentifier fid) { return; }
            FieldCssClasses = CascadedEditContext?.FieldCssClass(fid) ?? "";
        }

        protected override string ClassNames => ClassBuilder
        .Add("form-control")
        .AddIf("form-control-flush", DisplayMode == DisplayMode.Flush)
        .AddIf(FieldCssClasses, IsValid)
        .AddIf("is-invalid", !IsValid)
        .ToString();

        public void Dispose()
        {
            if (CascadedEditContext != null)
            {
                CascadedEditContext.OnValidationStateChanged -= SetValidationClasses;
            }
        }
    }
}
