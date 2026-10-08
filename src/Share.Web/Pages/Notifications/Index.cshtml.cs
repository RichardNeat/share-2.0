using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Share.Web.Models;
using Share.Web.Services;

namespace Share.Web.Pages.Notifications;

public class IndexModel(INotificationService notificationService) : PageModel
{
    private readonly INotificationService _notificationService = notificationService;
    public List<Notification> Notifications { get; set; } = [];
    public int UnreadCount { get; set; }

    public async Task OnGetAsync()
    {
        // TODO: Get current user ID from authentication context
        var userId = ""; // Placeholder - replace with actual user ID

        Notifications = await _notificationService.GetAllNotificationsAsync(userId);
        UnreadCount = await _notificationService.GetUnreadCountAsync(userId);
    }

    public async Task<IActionResult> OnPostMarkAsReadAsync(int id)
    {
        // TODO: Implement mark as read
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkAllAsReadAsync()
    {
        // TODO: Get current user ID and mark all as read
        return RedirectToPage();
    }
}
