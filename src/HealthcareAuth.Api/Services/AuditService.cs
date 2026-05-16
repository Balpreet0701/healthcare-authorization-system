using System.Security.Claims;
using HealthcareAuth.Api.Data;
using HealthcareAuth.Api.Models;

namespace HealthcareAuth.Api.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task WriteAsync(string action, string entityName, string entityId, string details, CancellationToken cancellationToken = default)
    {
        var context = _httpContextAccessor.HttpContext;
        var user = context?.User;

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = user?.FindFirstValue(ClaimTypes.NameIdentifier),
            UserName = user?.FindFirstValue(ClaimTypes.Name) ?? user?.Identity?.Name ?? "system",
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            IpAddress = context?.Connection.RemoteIpAddress?.ToString() ?? string.Empty
        });

        await _db.SaveChangesAsync(cancellationToken);
    }
}
