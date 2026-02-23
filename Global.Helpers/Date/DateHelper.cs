namespace Global.Helpers.Date;

public static class DateHelper
{
    public static DateTime ToDateTime(this DateOnly date) => new(date.Year, date.Month, date.Day);
    public static DateOnly ToDateOnly(this DateTime date) => new(date.Year, date.Month, date.Day);
}