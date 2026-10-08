using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Share.Web.Models;
using Share.Web.Services;

namespace Share.Web.Pages.Admin.Announcements;

public class CreateModel(IAnnouncementService announcementService) : PageModel
{
    private readonly IAnnouncementService _announcementService = announcementService;

    [BindProperty]
    public string Title { get; set; } = "";

    [BindProperty]
    public string Body { get; set; } = "";

    [BindProperty]
    public string? Link { get; set; }

    [BindProperty]
    public DateTime? PublishAt { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var announcement = new Announcement
        {
            Title = Title,
            Body = Body,
            Link = Link,
            PublishAt = PublishAt,
            IsHidden = false
        };

        await _announcementService.CreateAsync(announcement);
        return RedirectToPage("Index");
    }
}
