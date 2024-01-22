using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OffPlanner.Web.Pages;

using System.Globalization;

public class Countries : PageModel
{
    public IEnumerable<Locale> Locales { get; set; }

    public void OnGet()
    {
        Locales = AvailableLocales.List.Value.Select(x => new Locale
            {
                Identifier = x,
                Name = new RegionInfo(x).DisplayName
            })
            .OrderBy(x => x.Name);
    }

    public struct Locale
    {
        public string Identifier { get; set; }
        public string Name { get; set; }
    }
}