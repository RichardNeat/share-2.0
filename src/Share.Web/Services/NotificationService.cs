using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;

namespace Share.Web.Services;

public interface INotificationService
{
    Task CreateNotificationAsync(Notification notification);
    Task<List<Notification>> GetUnreadNotificationsAsync(string userId);
    Task<List<Notification>> GetAllNotificationsAsync(string userId);
    Task MarkAsReadAsync(int notificationId);
    Task MarkAllAsReadAsync(string userId);
    Task<int> GetUnreadCountAsync(string userId);
}

public class NotificationService(AppDbContext context) : INotificationService
{
    public async Task CreateNotificationAsync(Notification notification)
    {
        notification.CreatedAt = DateTime.UtcNow;
        notification.IsRead = false;
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();
    }

    public async Task<List<Notification>> GetUnreadNotificationsAsync(string userId)
    {
        return await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Notification>> GetAllNotificationsAsync(string userId)
    {
        return await context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        var notification = await context.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            await context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        var notifications = await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }
        await context.SaveChangesAsync();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();
    }
}
