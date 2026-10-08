using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Share.Web.Models;
using Share.Web.Services;

namespace Share.Web.Pages.Admin.Announcements;

public class IndexModel(IAnnouncementService announcementService) : PageModel
{
    private readonly IAnnouncementService _announcementService = announcementService;
    public List<Announcement> Announcements { get; set; } = [];

    public async Task OnGetAsync()
    {
        Announcements = await _announcementService.GetAllAnnouncementsAsync();
    }

    public async Task<IActionResult> OnPostHideAsync(int id)
    {
        await _announcementService.HideAsync(id);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostShowAsync(int id)
    {
        await _announcementService.ShowAsync(id);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        await _announcementService.DeleteAsync(id);
        return RedirectToPage();
    }
}
