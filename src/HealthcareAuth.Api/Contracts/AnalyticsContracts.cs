namespace HealthcareAuth.Api.Contracts;

public record StatusCountResponse(string Status, int Count);

public record AnalyticsResponse(
    int TotalRequests,
    int PendingReview,
    int Approved,
    int Denied,
    int DocumentsProcessed,
    double AverageTurnaroundHours,
    IReadOnlyCollection<StatusCountResponse> StatusCounts,
    IReadOnlyCollection<StatusCountResponse> PriorityCounts);

public record AuditLogResponse(
    int Id,
    string UserName,
    string Action,
    string EntityName,
    string EntityId,
    string Details,
    string IpAddress,
    DateTime CreatedAt);

public record NotificationResponse(
    int Id,
    string Title,
    string Message,
    string Link,
    bool IsRead,
    DateTime CreatedAt);
