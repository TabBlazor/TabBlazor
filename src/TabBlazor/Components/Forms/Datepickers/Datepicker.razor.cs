using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace TabBlazor
{
    public partial class Datepicker<TValue> : TablerBaseComponent
    {
        [Parameter] public bool Inline { get; set; }
        [Parameter] public string Format { get; set; } = "d";
        [Parameter] public TValue SelectedDate { get; set; }
        [Parameter] public EventCallback<TValue> SelectedDateChanged { get; set; }
        [Parameter] public string Label { get; set; }

        private TValue value;
        private DateTimeOffset currentDate = DateTimeOffset.Now;
        private DateTimeOffset? selectedDate;
        private TablerColor selectedColor = TablerColor.Primary;
        private CultureInfo culture => CultureInfo.CurrentCulture;

        private Dropdown dropdown;

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
                throw new SystemException("Type must be of type DateTime or DateTimeOffset");
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
            if (selectedDate == null || date == null) { return false; }
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
