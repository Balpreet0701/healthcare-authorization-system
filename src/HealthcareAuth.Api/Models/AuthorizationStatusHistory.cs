namespace HealthcareAuth.Api.Models;

public class AuthorizationStatusHistory
{
    public int Id { get; set; }
    public int AuthorizationRequestId { get; set; }
    public AuthorizationRequest? AuthorizationRequest { get; set; }
    public AuthorizationStatus FromStatus { get; set; }
    public AuthorizationStatus ToStatus { get; set; }
    public string? ChangedById { get; set; }
    public ApplicationUser? ChangedBy { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
