using Microsoft.EntityFrameworkCore;
using Share.Web.Data;
using Share.Web.Models;

namespace Share.Web.Services;

public interface IAnnouncementService
{
    Task<List<Announcement>> GetPublishedAnnouncementsAsync();
    Task<List<Announcement>> GetAllAnnouncementsAsync();
    Task<Announcement?> GetByIdAsync(int id);
    Task CreateAsync(Announcement announcement);
    Task UpdateAsync(Announcement announcement);
    Task HideAsync(int id);
    Task ShowAsync(int id);
    Task DeleteAsync(int id);
}

public class AnnouncementService(AppDbContext context, TimeProvider timeProvider) : IAnnouncementService
{
    public async Task<List<Announcement>> GetPublishedAnnouncementsAsync()
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await context.Announcements
            .Where(a => !a.IsHidden && (a.PublishAt == null || a.PublishAt <= now))
            .OrderByDescending(a => a.PublishAt ?? a.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Announcement>> GetAllAnnouncementsAsync()
    {
        return await context.Announcements
            .OrderByDescending(a => a.PublishAt ?? a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Announcement?> GetByIdAsync(int id)
    {
        return await context.Announcements.FindAsync(id);
    }

    public async Task CreateAsync(Announcement announcement)
    {
        announcement.CreatedAt = timeProvider.GetUtcNow().UtcDateTime;
        context.Announcements.Add(announcement);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Announcement announcement)
    {
        context.Announcements.Update(announcement);
        await context.SaveChangesAsync();
    }

    public async Task HideAsync(int id)
    {
        var announcement = await GetByIdAsync(id);
        if (announcement != null)
        {
            announcement.IsHidden = true;
            await UpdateAsync(announcement);
        }
    }

    public async Task ShowAsync(int id)
    {
        var announcement = await GetByIdAsync(id);
        if (announcement != null)
        {
            announcement.IsHidden = false;
            await UpdateAsync(announcement);
        }
    }

    public async Task DeleteAsync(int id)
    {
        var announcement = await GetByIdAsync(id);
        if (announcement != null)
        {
            context.Announcements.Remove(announcement);
            await context.SaveChangesAsync();
        }
    }
}
