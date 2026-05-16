namespace HealthcareAuth.Api.Models;

public class AuthorizationRequest
{
    public int Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public int PatientId { get; set; }
    public Patient? Patient { get; set; }
    public string RequestedService { get; set; } = string.Empty;
    public string DiagnosisCode { get; set; } = string.Empty;
    public string ProcedureCode { get; set; } = string.Empty;
    public PriorityLevel Priority { get; set; } = PriorityLevel.Routine;
    public AuthorizationStatus Status { get; set; } = AuthorizationStatus.Draft;
    public string ClinicalNotes { get; set; } = string.Empty;
    public string? CreatedById { get; set; }
    public ApplicationUser? CreatedBy { get; set; }
    public string? AssignedReviewerId { get; set; }
    public ApplicationUser? AssignedReviewer { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public string? AiSummary { get; set; }
    public string? AiRecommendation { get; set; }
    public decimal? AiConfidenceScore { get; set; }
    public string? AiRationale { get; set; }
    public string? DecisionReason { get; set; }
    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<MedicalDocument> Documents { get; set; } = new List<MedicalDocument>();
    public ICollection<UrlAttachment> UrlAttachments { get; set; } = new List<UrlAttachment>();
    public ICollection<AuthorizationStatusHistory> StatusHistory { get; set; } = new List<AuthorizationStatusHistory>();
}
