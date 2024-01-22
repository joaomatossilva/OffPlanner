namespace OffPlanner.Web.Pages;

using System.Reflection;
using DateTimeExtensions.Common;
using DateTimeExtensions.WorkingDays;
using DateTimeExtensions.WorkingDays.RegionIdentifiers;

public class AvailableLocales
{
    public static Lazy<HashSet<string>> List { get; } = new(() =>
        Assembly.GetAssembly(typeof(IWorkingDayCultureInfo))?
            .GetTypes()
            .Where(x => x.IsClass && x.IsAssignableTo(typeof(IHolidayStrategy)))
            .SelectMany(x => x.GetCustomAttributes<LocaleAttribute>().Select(y => y.Locale)).ToHashSet()
        ?? new HashSet<string>());

    public static IDictionary<string, HashSet<string>> Regions { get; } = new Dictionary<string, HashSet<string>>
    {
        { "pt-PT", new HashSet<string> { PortugalRegion.Lisboa, PortugalRegion.Porto, PortugalRegion.CasteloBranco } }
    };
}