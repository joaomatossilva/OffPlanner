using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OffPlanner.Web.Pages;

using Microsoft.AspNetCore.Mvc;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public IActionResult OnGet()
    {
        var userLanguages = Request.GetTypedHeaders()
            .AcceptLanguage
            .OrderByDescending(x => x.Quality ?? 1)
            .Select(x => x.Value.ToString());

        foreach (var userLanguage in userLanguages)
        {
            if (userLanguage.Length == 2)
            {
                var foundMatch = AvailableLocales.List.Value.FirstOrDefault(x => x.Substring(0, 2) == userLanguage);
                if (foundMatch != null)
                {
                    return Redirect(Url.Page("Suggestions", new { locale = foundMatch }));
                }
            }

            if (AvailableLocales.List.Value.Contains(userLanguage))
            {
                return Redirect(Url.Page("Suggestions", new { locale = userLanguage }));
            }
        }

        return Redirect(Url.Page("Suggestions", new { locale = "pt-PT" }));
    }
}