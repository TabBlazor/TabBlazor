using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TabBlazor
{
    /// <summary>
    /// A range/slider input supporting either a single value or multiple drag points, over a numeric range
    /// (<see cref="Min"/>/<see cref="Max"/>/<see cref="Step"/>) or a discrete <see cref="Range"/> of values.
    /// <typeparamref name="TValue"/> is the value type.
    /// </summary>
    public partial class RangeSlider<TValue> : ComponentBase
    {
        /// <summary>The single selected value. Supports two-way binding via <c>@bind-Value</c>.</summary>
        [Parameter] public TValue Value { get; set; }
        /// <summary>Raised when the single value changes.</summary>
        [Parameter] public EventCallback<TValue> ValueChanged { get; set; }
        /// <summary>The selected values when using multiple drag points. Supports <c>@bind-Values</c>.</summary>
        [Parameter] public List<TValue> Values { get; set; }
        /// <summary>Raised when the multi-point values change.</summary>
        [Parameter] public EventCallback<List<TValue>> ValuesChanged { get; set; }
        /// <summary>Discrete set of selectable values. Mutually exclusive with <see cref="Min"/>/<see cref="Max"/>/<see cref="Step"/>.</summary>
        [Parameter] public List<TValue> Range { get; set; }
        /// <summary>Projects a value to its display label.</summary>
        [Parameter] public Func<TValue, string> LabelExpression { get; set; }

        /// <summary>Minimum value. Defaults to 0. Not used with <see cref="Range"/>.</summary>
        [Parameter] public double Min { get; set; } = 0.0;
        /// <summary>Maximum value. Defaults to 10. Not used with <see cref="Range"/>.</summary>
        [Parameter] public double Max { get; set; } = 10.0;
        /// <summary>Step increment. Defaults to 1. Cannot be combined with <see cref="Range"/>.</summary>
        [Parameter] public double Step { get; set; } = 1.0;

        /// <summary>When true, shows min/max labels. Defaults to false.</summary>
        [Parameter] public bool ShowLabels { get; set; } = false;
        /// <summary>Unmatched HTML attributes applied to the slider input.</summary>
        [Parameter(CaptureUnmatchedValues = true)] public IDictionary<string, object> UnmatchedParameters { get; set; }

        private bool SingleDragPoint => Values == null || !Values.Any();

        private TValue currentValue;
        private List<TValue> currentValues = new();

        private int currentIndex;
        private readonly List<int> currentIndices = new();
        private int minIndex;
        private int maxIndex;


        protected override void OnInitialized()
        {
            base.OnInitialized();

            ThrowIfInvalidOptions();

            PopulateInternalValues();
        }

        private void ThrowIfInvalidOptions()
        {
            if (Step != 1.0 && Range != null && Range.Any())
            {
                throw new InvalidOperationException("Step and Range can't be used together. Pick one or the other.");
            }

            if (Range != null && Range.Any() && (Min != 0.0 || Max != 10.0))
            {
                throw new InvalidOperationException("You can't set a Max and/or Min value when using Range.");
            }
        }

        private void PopulateInternalValues()
        {
            if (SingleDragPoint)
            {
                PopulateSingleDragPointValues();
            }
            else
            {
                PopulateMultiDragPointValues();
            }
        }

        private void PopulateSingleDragPointValues()
        {
            if (Range != null)
            {
                currentIndex = Range.IndexOf(Value);
                minIndex = 0;
                maxIndex = Range.Count - 1;
            }
            else
            {
                currentValue = Value;
            }
        }

        private void PopulateMultiDragPointValues()
        {
            if (Range != null)
            {
                minIndex = 0;
                maxIndex = Range.Count - 1;
                foreach (var item in Values)
                {
                    currentIndices.Add(Values.IndexOf(item));
                }
            }

            currentValues = Values;
        }

        private static string GetSliderAttribute(double value)
        {
            return value.ToString().Replace(",", ".");
        }

        private static string GetSliderAttributeGeneric(TValue value)
        {
            return value.ToString().Replace(",", ".");
        }

        private string GetMinLabel()
        {
            if (Range != null)
            {
                return LabelExpression != null ?
                    LabelExpression(Range[minIndex]) :
                    Range[minIndex].ToString();
            }

            return Min.ToString();
        }

        private string GetMaxLabel()
        {
            if (Range != null)
            {
                return LabelExpression != null ?
                    LabelExpression(Range[maxIndex]) :
                    Range[maxIndex].ToString();
            }

            return Max.ToString();
        }

        private void HandleRangeOnChange(ChangeEventArgs e)
        {
            if (e.Value is not string valueString)
            {
                return;
            }

            if (!int.TryParse(valueString, out int newSliderValueIndex))
            {
                return;
            }

            currentIndex = newSliderValueIndex;
            var item = Range[currentIndex];

            if (ValueChanged.HasDelegate)
            {
                ValueChanged.InvokeAsync(item);
            }
        }

        private void HandleRangeOnChangeMulti(ChangeEventArgs e, int sliderIndex)
        {
            if (e.Value is not string valueString)
            {
                return;
            }

            if (!int.TryParse(valueString, out int newSliderValueIndex))
            {
                return;
            }

            currentIndices[sliderIndex] = newSliderValueIndex;
            var item = Range[newSliderValueIndex];

            currentValues[sliderIndex] = item;

            if (ValuesChanged.HasDelegate)
            {
                ValuesChanged.InvokeAsync(currentValues);
            }
        }

        private void HandleValueOnChange(ChangeEventArgs e)
        {
            if (ValueChanged.HasDelegate && e.Value is object valueObj)
            {
                var valueString = valueObj.ToString().Replace(".", ",");
                var convertedObj = Convert.ChangeType(valueString, typeof(TValue));
                ValueChanged.InvokeAsync((TValue)convertedObj);
            }
        }

        private void HandleValueOnChangeMulti(ChangeEventArgs e, int sliderIndex)
        {
            if (ValuesChanged.HasDelegate && e.Value is object valueObj)
            {
                var valueString = valueObj.ToString().Replace(".", ",");
                var convertedObj = Convert.ChangeType(valueString, typeof(TValue));

                currentValues[sliderIndex] = (TValue)convertedObj;

                ValuesChanged.InvokeAsync(currentValues);
            }
        }
    }
}
