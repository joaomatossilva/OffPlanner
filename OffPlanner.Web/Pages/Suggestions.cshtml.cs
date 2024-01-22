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

    public void OnGet(string locale, string region)
    {
        Locale = locale;
        var regionInfo = new RegionInfo(locale);
        CountryName = regionInfo.DisplayName;

        Regions = AvailableLocales.Regions.TryGetValue(locale, out var regions)
            ? regions
            : new HashSet<string>();

        Calculate(locale, region);
    }


    public void Calculate(string locale, string region)
    {
        var cultureInfo = new WorkingDayCultureInfo(locale, region);
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
                        Ratio = p.Score * (15/100f * t.Key)
                    })
                    .OrderByDescending(p => p.Ratio)
            })
            .OrderByDescending(t => t.TotalDaysOff);
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