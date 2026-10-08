using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Share.Web.Services;

namespace Share.Web.Pages;

public class DownloadsModel : PageModel
{
    private readonly DownloadService _downloadService;

    [BindProperty]
    public string? RecordType { get; set; }

    public DownloadsModel(DownloadService downloadService)
    {
        _downloadService = downloadService;
    }

    public IActionResult OnGet()
    {
        return Page();
    }

    public IActionResult OnPost()
    {
        if (string.IsNullOrEmpty(RecordType))
        {
            return Page();
        }

        var userCouncil = User.FindFirst("council")?.Value;
        var (filename, csv) = RecordType switch
        {
            "guests" => _downloadService.ExportGuests(userCouncil),
            "people" => _downloadService.ExportPeople(userCouncil),
            "accommodations" => _downloadService.ExportAccommodations(userCouncil),
            "cases" => _downloadService.ExportCases(userCouncil),
            _ => ("unknown.csv", "")
        };

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", filename);
    }
}
