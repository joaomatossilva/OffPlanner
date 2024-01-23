namespace OffPlanner.Web;

using DateTimeExtensions.WorkingDays;

public class CustomWorkingDayCultureInfoDecorator(IWorkingDayCultureInfo innerWorkingDayCultureInfo, HashSet<DateTime> extraDays) : IWorkingDayCultureInfo
{
    public bool IsHoliday(DateTime date)
    {
        if (extraDays.Contains(date))
        {
            return true;
        }

        return innerWorkingDayCultureInfo.IsHoliday(date);
    }

    public bool IsWorkingDay(DateTime date)
    {
        return !this.IsHoliday(date) && innerWorkingDayCultureInfo.IsWorkingDay(date);
    }

    public bool IsWorkingDay(DayOfWeek dayOfWeek)
    {
        return innerWorkingDayCultureInfo.IsWorkingDay(dayOfWeek);
    }

    public IEnumerable<Holiday> GetHolidaysOfYear(int year)
    {
        return innerWorkingDayCultureInfo.GetHolidaysOfYear(year)
            .Concat(extraDays.Select(x => new FixedHoliday("extra-day", x.Month, x.Day)));
    }

    public IEnumerable<Holiday> Holidays => innerWorkingDayCultureInfo.Holidays
        .Concat(extraDays.Select(x => new FixedHoliday("extra-day", x.Month, x.Day)));

    public string Name => innerWorkingDayCultureInfo.Name;
}