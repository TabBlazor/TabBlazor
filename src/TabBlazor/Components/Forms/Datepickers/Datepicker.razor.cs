using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;

namespace TabBlazor
{
    /// <summary>
    /// Calendar-based date picker bound to a <see cref="DateTime"/> or <see cref="DateTimeOffset"/> value
    /// (or their nullable variants). Renders either inline or inside a dropdown opened from a text input.
    /// </summary>
    public partial class Datepicker<TValue> : TablerBaseComponent
    {
        /// <summary>When true, renders the calendar inline instead of inside a dropdown. Defaults to false.</summary>
        [Parameter] public bool Inline { get; set; }
        /// <summary>The .NET date format string used to display the selected date in the input. Defaults to "d" (short date).</summary>
        [Parameter] public string Format { get; set; } = "d";
        /// <summary>The currently selected date. <typeparamref name="TValue"/> must be <see cref="DateTime"/> or <see cref="DateTimeOffset"/> (or nullable).</summary>
        [Parameter] public TValue SelectedDate { get; set; }
        /// <summary>Invoked when the selected date changes; enables two-way binding via @bind-SelectedDate.</summary>
        [Parameter] public EventCallback<TValue> SelectedDateChanged { get; set; }
        /// <summary>Expression identifying the bound field, used for validation inside an <see cref="EditContext"/>.</summary>
        [Parameter] public Expression<Func<TValue>> SelectedDateExpression { get; set; }
        /// <summary>Optional label rendered above the picker.</summary>
        [Parameter] public string Label { get; set; }
        /// <summary>Controls how the input is styled. Defaults to <see cref="DisplayMode.Default"/>.</summary>
        [Parameter] public DisplayMode DisplayMode { get; set; } = DisplayMode.Default;
        [CascadingParameter] private EditContext CascadedEditContext { get; set; }

        private string FieldCssClasses { get; set; }
        private string InputCssClasses => new ClassBuilder()
            .Add("form-control cursor-pointer")
            .Add(FieldCssClasses)
            .AddIf("form-control-flush", DisplayMode == DisplayMode.Flush)
            .ToString();
        private FieldIdentifier? fieldIdentifier;
        private TValue value;
        private DateTimeOffset currentDate = DateTimeOffset.Now;
        private DateTimeOffset? selectedDate;
        private TablerColor selectedColor = TablerColor.Primary;
        private CultureInfo culture => CultureInfo.CurrentCulture;

        private Dropdown dropdown;

        protected override void OnInitialized()
        {
            if (SelectedDateExpression != null)
            {
                fieldIdentifier = FieldIdentifier.Create(SelectedDateExpression);
            }

            if (CascadedEditContext != null)
            {
                CascadedEditContext.OnValidationStateChanged += SetValidationClasses;
            }
        }

        protected override void OnAfterRender(bool firstRender)
        {
            Validate();
        }

        private void Validate()
        {
            if (fieldIdentifier is not { } fid)
            {
                return;
            }
            CascadedEditContext?.NotifyFieldChanged(fid);
            CascadedEditContext?.Validate();
        }
        
        private void SetValidationClasses(object sender, ValidationStateChangedEventArgs args)
        {
            if (fieldIdentifier is not { } fid)
            {
                return;
            }

            FieldCssClasses = CascadedEditContext?.FieldCssClass(fid) ?? "";
        }


        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            if (!EqualityComparer<TValue>.Default.Equals(value, SelectedDate))
            {
                value = SelectedDate;

                await SetSelected(ConvertToDateTimeOffset(SelectedDate));
            }
        }

        private TValue ConvertToTValue(DateTimeOffset? value)
        {
            var type = typeof(TValue);
            if (type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?))
            {
                return (TValue)(object)value;
            }
            else if (type == typeof(DateTime) || type == typeof(DateTime?))
            {
                return (TValue)(object)value?.DateTime;
            }

            return default;
        }

        private DateTimeOffset? ConvertToDateTimeOffset(TValue value)
        {
            var type = typeof(TValue);
            if (type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?))
            {
                return value as DateTimeOffset?;
            }
            else if (type == typeof(DateTime) || type == typeof(DateTime?))
            {
                var dateTime = value as DateTime?;
                DateTimeOffset? newDate = dateTime;
                return newDate;
            }

            else
            {
                throw new SystemException("BadgeType must be of type DateTime or DateTimeOffset");
            }
        }

        private string[] GetWeekdays()
        {
            var names = culture.DateTimeFormat.AbbreviatedDayNames;
            var first = (int)culture.DateTimeFormat.FirstDayOfWeek;
            return names.Skip(first).Take(names.Length - first).Concat(names.Take(first)).ToArray();
        }

        private string GetCurrentMonth()
        {
            return currentDate.ToString("MMMM", culture.DateTimeFormat);
        }

        private void SetPreviousMonth()
        {
            currentDate = currentDate.AddMonths(-1);
        }

        private void SetNextMonth()
        {
            currentDate = currentDate.AddMonths(1);
        }

        private DateTimeOffset FirstDateInWeek(DateTimeOffset dt)
        {
            while (dt.DayOfWeek != culture.DateTimeFormat.FirstDayOfWeek)
                dt = dt.AddDays(-1);
            return dt;
        }

        private List<DateTimeOffset> GetDates()
        {
            var dates = new List<DateTimeOffset>();
            var firstDayOfMonth = currentDate.Date.AddDays(1 - currentDate.Day);
            var firstDate = FirstDateInWeek(firstDayOfMonth);
            for (int i = 0; i < 42; i++)
            {
                dates.Add(firstDate);
                firstDate = firstDate.AddDays(1);
            }

            return dates;
        }

        private async Task SetSelected(DateTimeOffset? date)
        {
            selectedDate = date;
            if (date != null && !IsCurrentMonth(date))
            {
                currentDate = (DateTimeOffset)date;
            }

            value = ConvertToTValue(selectedDate);

            await SelectedDateChanged.InvokeAsync(value);
            Validate();
            if (!Inline && dropdown != null)
            {
                dropdown.Close();
            }
        }

        private bool IsCurrentMonth(DateTimeOffset? date)
        {
            return date?.Month == currentDate.Month;
        }

        private bool IsSelected(DateTimeOffset? date)
        {
            if (selectedDate == null || date == null)
            {
                return false;
            }

            return selectedDate?.Date == date?.Date;
        }

        private string DayCss(DateTimeOffset? date)
        {
            return new ClassBuilder()
                .Add("datepicker-day")
                .AddIf("datepicker-not-month", !IsCurrentMonth(date))
                .AddIf("datepicker-day-dropdown", !Inline)
                .AddIf("strong", date?.Date == DateTimeOffset.Now.Date)
                .AddIf(selectedColor.GetColorClass("bg") + " text-white", IsSelected(date))
                .ToString();
        }
    }
}