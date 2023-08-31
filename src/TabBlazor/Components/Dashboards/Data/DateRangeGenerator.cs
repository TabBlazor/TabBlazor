using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TabBlazor.Dashboards.Extensions;
namespace TabBlazor.Dashboards
{
    public static class DateRangeGenerator
    {

        public static List<DateRange> Generate()
        {
            var ranges = new List<DateRange>
            {
                Yesterday(),
                ThisWeek(),
                LastWeek(),
                ThisMonth(),
                LastMonth(),
                ThisYear(),
                LastYear()
            };

            return ranges;
        }


        public static DateRange Yesterday()
        {
            var date = DateTime.Today.AddDays(-1);
            return new DateRange("Yesterday", date, date.EndOfDay());
        }

        public static DateRange ThisWeek()
        {
            var date = DateTime.Today.StartOfWeek(DayOfWeek.Monday);
            return new DateRange("This Week", date, date.AddDays(7).EndOfDay());
        }

        public static DateRange LastWeek()
        {
            var date = DateTime.Today.AddDays(-7).StartOfWeek(DayOfWeek.Monday);
            return new DateRange("Last Week", date, date.AddDays(7).EndOfDay());
        }

        public static DateRange ThisMonth()
        {
            var date = DateTime.Today.StartOfMonth();
            return new DateRange("This Month", date, date.EndOfMonth());
        }

        public static DateRange LastMonth()
        {
            var date = DateTime.Today.AddMonths(-1).StartOfMonth();
            return new DateRange("Last Month", date, date.EndOfMonth());
        }

        public static DateRange ThisYear()
        {
            var date = DateTime.Today.StartOfYear();
            return new DateRange("This Year", date, date.EndOfYear());
        }

        public static DateRange LastYear()
        {
            var date = DateTime.Today.AddYears(-1).StartOfYear();
            return new DateRange("Last Year", date, date.EndOfYear());
        }

    }

   


    public class DateRange
    {
        public DateRange()
        {}

        public DateRange(string name, DateTime start, DateTime end)
        {
            Name = name;
            Start = start;
            End = end;
        }

        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public string Name { get; set; }
    }

}
