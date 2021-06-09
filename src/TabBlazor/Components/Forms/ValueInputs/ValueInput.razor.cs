using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TabBlazor
{
    public partial class ValueInput<TValue> : TablerBaseComponent
    {
        private string currentValue;

        [Parameter] public TValue Value { get; set; }
        [Parameter] public EventCallback<TValue> ValueChanged { get; set; }
        [Parameter] public string Label { get; set; }

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


                //if (IsValid)
                //{
                //    ValueChanged.InvokeAsync(newValue);
                //}



            }
        }

        protected override void OnParametersSet()
        {
            currentValue = Value.ToString();
            base.OnParametersSet();
        }

        protected override string ClassNames => ClassBuilder
        .Add("form-control")
        .AddIf("is-invalid", !IsValid)
        .ToString();


    }
}
