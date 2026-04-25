using ERPSystem.Data.Context;
using ERPSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class NotificationsService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public NotificationsService(
        ApplicationDbContext context,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    private string GetUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        var userId =
            user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            user?.FindFirst("sub")?.Value ??
            user?.FindFirst("uid")?.Value;

        if (string.IsNullOrWhiteSpace(userId))
            throw new UnauthorizedAccessException("Utilizatorul nu este autentificat.");

        return userId;
    }

    public async Task<List<Notification>> GetMyNotifications()
    {
        var userId = GetUserId();

        return await _context.Notifications
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Take(20)
            .ToListAsync();
    }

    public async Task<bool> MarkAsRead(int id)
    {
        var userId = GetUserId();

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (notification == null)
            return false;

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> MarkAllAsRead()
    {
        var userId = GetUserId();

        var notifications = await _context.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> MarkAllAsSeen()
    {
        var userId = GetUserId();

        var notifications = await _context.Notifications
            .Where(x => x.UserId == userId && x.SeenAt == null)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.SeenAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<int> GetUnreadCount()
    {
        var userId = GetUserId();

        return await _context.Notifications
            .CountAsync(x => x.UserId == userId && !x.IsRead);
    }


    public async Task CreateNotificationAsync( string userId, string eventType, string title,string message, string type = "Info",  string? link = null,  string? entityType = null, string? entityId = null)
    {
        var setting = await _context.UserNotificationSettings
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.EventType == eventType &&
                x.Channel == NotificationChannel.InApp);

        if (setting != null && !setting.Enabled)
            return;

        _context.Notifications.Add(new Notification
        {
            UserId = userId,
            EventType = eventType,
            Title = title,
            Message = message,
            Type = type,
            Link = link,
            EntityType = entityType,
            EntityId = entityId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }
}