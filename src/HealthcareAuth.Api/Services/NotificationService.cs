using HealthcareAuth.Api.Contracts;
using HealthcareAuth.Api.Data;
using HealthcareAuth.Api.Hubs;
using HealthcareAuth.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace HealthcareAuth.Api.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHubContext<NotificationsHub> _hubContext;

    public NotificationService(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IHubContext<NotificationsHub> hubContext)
    {
        _db = db;
        _userManager = userManager;
        _hubContext = hubContext;
    }

    public async Task NotifyUserAsync(string userId, string title, string message, string link, CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Link = link
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(cancellationToken);

        await _hubContext.Clients.User(userId).SendAsync("notification", ToResponse(notification), cancellationToken);
    }

    public async Task NotifyRoleAsync(string role, string title, string message, string link, CancellationToken cancellationToken = default)
    {
        var users = await _userManager.GetUsersInRoleAsync(role);
        var notifications = users.Select(user => new Notification
        {
            UserId = user.Id,
            Title = title,
            Message = message,
            Link = link
        }).ToList();

        if (notifications.Count > 0)
        {
            _db.Notifications.AddRange(notifications);
            await _db.SaveChangesAsync(cancellationToken);
        }

        await _hubContext.Clients.Group(role).SendAsync("notification", new
        {
            title,
            message,
            link,
            createdAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task<IReadOnlyCollection<NotificationResponse>> GetUnreadAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _db.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .OrderByDescending(x => x.CreatedAt)
            .Take(20)
            .Select(x => ToResponse(x))
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(int notificationId, string userId, CancellationToken cancellationToken = default)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId, cancellationToken);

        if (notification is null)
        {
            return;
        }

        notification.IsRead = true;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static NotificationResponse ToResponse(Notification notification)
    {
        return new NotificationResponse(
            notification.Id,
            notification.Title,
            notification.Message,
            notification.Link,
            notification.IsRead,
            notification.CreatedAt);
    }
}
