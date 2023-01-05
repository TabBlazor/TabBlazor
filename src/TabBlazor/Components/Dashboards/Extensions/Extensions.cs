

namespace TabBlazor.Dashboards.Extensions
{
    public static class Extensions
    {


        public static DateTime StartOfYear(this DateTime date)
        {
            return new(date.Year, 1, 1);
        }

        public static DateTime StartOfMonth(this DateTime date)
        {
            return new(date.Year, date.Month, 1);
        }

        public static DateTime StartOfWeek(this DateTime date, DayOfWeek day)
        {
            do { date = date.AddDays(-1).StartOfDay(); }
            while (date.DayOfWeek != day); 
            return date;
        }

        public static DateTime StartOfDay(this DateTime date)
        {
            return new(date.Year, date.Month, date.Day);
        }


        public static DateTime EndOfYear(this DateTime date)
        {
            date = StartOfYear(date);
            return date.AddYears(1).AddTicks(-1);
        }

        public static DateTime EndOfMonth(this DateTime date)
        {
            return date.StartOfDay().AddMonths(1).AddTicks(-1);
        }

        public static DateTime EndOfDay(this DateTime date)
        {
            return date.StartOfDay().AddDays(1).AddTicks(-1);
        }

        
    }
}
