namespace HealthcareAuth.Api.Models;

public class MedicalDocument
{
    public int Id { get; set; }
    public int AuthorizationRequestId { get; set; }
    public AuthorizationRequest? AuthorizationRequest { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public OcrStatus OcrStatus { get; set; } = OcrStatus.Pending;
    public string? OcrText { get; set; }
    public string? OcrError { get; set; }
    public string? UploadedById { get; set; }
    public ApplicationUser? UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
