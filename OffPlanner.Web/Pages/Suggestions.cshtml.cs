using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OffPlanner.Web.Pages;

using System.Globalization;
using DateTimeExtensions;
using DateTimeExtensions.WorkingDays;

public class Suggestions : PageModel
{
    public IEnumerable<VacationsSuggestionsViewModel> VacationSuggestions { get; set; }
    public string CountryName { get; set; }
    public string Locale { get; set; }
    public HashSet<string> Regions { get; set; }
    public string Region { get; set; }
    public DateTime?[] Days { get; set; } = new DateTime?[5];

    public void OnGet(string locale, string region, DateTime?[]? day)
    {
        Locale = locale;
        var regionInfo = new RegionInfo(locale);
        CountryName = regionInfo.DisplayName;

        Regions = AvailableLocales.Regions.TryGetValue(locale, out var regions)
            ? regions
            : new HashSet<string>();

        if (day != null)
        {
            for (int i = 0; i < Days.Length; i++)
            {
                if (day.Length > i)
                {
                    Days[i] = day[i];
                }
            }
        }

        var extraDays = day?
            .Where(x => x != null)
            .Select(x => x.Value)
            .ToHashSet() ?? new HashSet<DateTime>();
        Calculate(locale, region, extraDays);
    }


    public void Calculate(string locale, string region, HashSet<DateTime> extraDays)
    {
        IWorkingDayCultureInfo cultureInfo = new WorkingDayCultureInfo(locale, region);
        if (extraDays.Count > 0)
        {
            cultureInfo = new CustomWorkingDayCultureInfoDecorator(cultureInfo, extraDays);
        }

        var year = 2024;

        VacationSuggestions = GetSuggestions(cultureInfo, year, 10)
            .GroupBy(s => s.TotalDays)
            .Select(t => new VacationsSuggestionsViewModel {
                TotalDaysOff = t.Key,
                Suggestions = t
                    .Select( p=> new VacationSuggestion {
                        StartDate = p.StartDate,
                        EndDate = p.EndDate,
                        VacationDaysSpent = p.WorkingDays,
                        Ratio = p.Score * (15/100f * t.Key),
                        Days = ExplainDays(cultureInfo, p.StartDate, p.EndDate)
                    })
                    .OrderByDescending(p => p.Ratio)
            })
            .OrderByDescending(t => t.TotalDaysOff);
    }

    public IEnumerable<Day> ExplainDays(IWorkingDayCultureInfo workingDayCultureInfo, DateTime startDate, DateTime endDate)
    {
        var date = startDate;
        do
        {
            var day = new Day
            {
                DateTime = date,
                IsWeekend = !workingDayCultureInfo.IsWorkingDay(date.DayOfWeek),
                IsHoliday = workingDayCultureInfo.IsHoliday(date)
            };
            yield return day;
            date = date.AddDays(1);
        } while (date <= endDate);
    }

    public class VacationsSuggestionsViewModel {
        public int TotalDaysOff { get; set; }
        public IEnumerable<VacationSuggestion> Suggestions { get; set; }
    }

    public class VacationSuggestion {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int VacationDaysSpent { get; set; }
        public float Ratio { get; set; }
        public IEnumerable<Day> Days { get; set; }
    }

    public class Day
    {
        public DateTime DateTime { get; set; }
        public string Name { get; set; }
        public bool IsWeekend { get; set; }
        public bool IsHoliday { get; set; }
    }

    public IEnumerable<VacationPeriod> GetSuggestions(IWorkingDayCultureInfo workingdayCultureInfo, int year, int maxDaysForward) {
        DateTime date = new DateTime(year, 1, 1);
        List<VacationPeriod> results = new List<VacationPeriod>();
        do {
            results.AddRange(CrawlPeriod(workingdayCultureInfo, date, maxDaysForward));
            date = date.AddDays(1);
        } while (date.Year <= year);

        return results.Where(s => s.Score > .50);
    }

    private static IEnumerable<VacationPeriod> CrawlPeriod(IWorkingDayCultureInfo workingdayCultureInfo, DateTime date, int maxDaysForward)
    {
        //Start crawling only if the last 2 days are working days
        if (date.IsWorkingDay(workingdayCultureInfo) || !date.AddDays(-1).IsWorkingDay(workingdayCultureInfo) || !date.AddDays(-2).IsWorkingDay(workingdayCultureInfo)) {
            yield break;
        }

        int workingDaysCount = date.IsWorkingDay(workingdayCultureInfo) ? 1 : 0;
        for (int i = 1; i <= maxDaysForward; i++) {
            DateTime endDate = date.AddDays(i);
            if (endDate.IsWorkingDay(workingdayCultureInfo)) {
                workingDaysCount++;
            }
            if (workingDaysCount == 0) {
                continue;
            }
            //end crawl only when the next 2 days are working days
            if (endDate.IsWorkingDay(workingdayCultureInfo) || !endDate.AddDays(1).IsWorkingDay(workingdayCultureInfo) || !endDate.AddDays(2).IsWorkingDay(workingdayCultureInfo)) {
                continue;
            }
            yield return new VacationPeriod { StartDate = date, EndDate = endDate, WorkingDays = workingDaysCount };
        }
    }

    public class VacationPeriod {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int WorkingDays { get; set; }
        public int TotalDays {
            get {
                return EndDate.Subtract(StartDate).Days +1; //the plus one is the end day inclusive
            }
        }
        public float Score {
            get {
                return 1 - (WorkingDays / (float)TotalDays);
            }
        }
    }
}