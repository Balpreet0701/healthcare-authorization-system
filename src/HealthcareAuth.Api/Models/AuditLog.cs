namespace HealthcareAuth.Api.Models;

public class AuditLog
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string UserName { get; set; } = "system";
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
