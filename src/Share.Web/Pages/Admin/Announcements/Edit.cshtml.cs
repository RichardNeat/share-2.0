using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Share.Web.Models;
using Share.Web.Services;

namespace Share.Web.Pages.Admin.Announcements;

public class EditModel(IAnnouncementService announcementService) : PageModel
{
    private readonly IAnnouncementService _announcementService = announcementService;

    [BindProperty]
    public Announcement Announcement { get; set; } = new() { Title = "", Body = "" };

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var announcement = await _announcementService.GetByIdAsync(id);
        if (announcement == null)
        {
            return NotFound();
        }

        Announcement = announcement;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _announcementService.UpdateAsync(Announcement);
        return RedirectToPage("Index");
    }
}
