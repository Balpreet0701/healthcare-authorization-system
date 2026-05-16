namespace HealthcareAuth.Api.Models;

public class UrlAttachment
{
    public int Id { get; set; }
    public int AuthorizationRequestId { get; set; }
    public AuthorizationRequest? AuthorizationRequest { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
