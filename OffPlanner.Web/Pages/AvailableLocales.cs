namespace OffPlanner.Web.Pages;

using System.Reflection;
using DateTimeExtensions.Common;
using DateTimeExtensions.WorkingDays;

public class AvailableLocales
{
    public static Lazy<HashSet<string>> List { get; } = new(() =>
        Assembly.GetAssembly(typeof(IWorkingDayCultureInfo))?
            .GetTypes()
            .Where(x => x.IsClass && x.IsAssignableTo(typeof(IHolidayStrategy)))
            .SelectMany(x => x.GetCustomAttributes<LocaleAttribute>().Select(y => y.Locale)).ToHashSet()
        ?? new HashSet<string>());
}