using HealthcareAuth.Api.Contracts;

namespace HealthcareAuth.Api.Services;

public interface INotificationService
{
    Task NotifyUserAsync(string userId, string title, string message, string link, CancellationToken cancellationToken = default);
    Task NotifyRoleAsync(string role, string title, string message, string link, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<NotificationResponse>> GetUnreadAsync(string userId, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(int notificationId, string userId, CancellationToken cancellationToken = default);
}
