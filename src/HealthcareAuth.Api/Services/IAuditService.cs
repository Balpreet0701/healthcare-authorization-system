namespace HealthcareAuth.Api.Services;

public interface IAuditService
{
    Task WriteAsync(string action, string entityName, string entityId, string details, CancellationToken cancellationToken = default);
}
