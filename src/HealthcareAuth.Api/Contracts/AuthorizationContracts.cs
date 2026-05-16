using HealthcareAuth.Api.Models;

namespace HealthcareAuth.Api.Contracts;

public record AuthorizationCreateRequest(
    int PatientId,
    string RequestedService,
    string DiagnosisCode,
    string ProcedureCode,
    PriorityLevel Priority,
    string ClinicalNotes,
    DateTime? DueDate);

public record AuthorizationUpdateRequest(
    string RequestedService,
    string DiagnosisCode,
    string ProcedureCode,
    PriorityLevel Priority,
    string ClinicalNotes,
    DateTime? DueDate);

public record ReviewDecisionRequest(
    AuthorizationStatus Decision,
    string Reason);

public record UrlAttachmentCreateRequest(
    string Title,
    string Url,
    string Description);

public record MedicalDocumentResponse(
    int Id,
    string FileName,
    string ContentType,
    long FileSize,
    OcrStatus OcrStatus,
    string? OcrText,
    string? OcrError,
    DateTime UploadedAt);

public record UrlAttachmentResponse(
    int Id,
    string Title,
    string Url,
    string Description,
    DateTime CreatedAt);

public record StatusHistoryResponse(
    AuthorizationStatus FromStatus,
    AuthorizationStatus ToStatus,
    string Reason,
    DateTime CreatedAt);

public record AuthorizationResponse(
    int Id,
    string RequestNumber,
    int PatientId,
    string PatientName,
    string MedicalRecordNumber,
    string RequestedService,
    string DiagnosisCode,
    string ProcedureCode,
    PriorityLevel Priority,
    AuthorizationStatus Status,
    string ClinicalNotes,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    DateTime? DueDate,
    string? AiSummary,
    string? AiRecommendation,
    decimal? AiConfidenceScore,
    string? AiRationale,
    string? DecisionReason,
    IReadOnlyCollection<MedicalDocumentResponse> Documents,
    IReadOnlyCollection<UrlAttachmentResponse> UrlAttachments,
    IReadOnlyCollection<StatusHistoryResponse> StatusHistory);

public record AuthorizationListItemResponse(
    int Id,
    string RequestNumber,
    string PatientName,
    string RequestedService,
    string DiagnosisCode,
    string ProcedureCode,
    PriorityLevel Priority,
    AuthorizationStatus Status,
    DateTime CreatedAt,
    DateTime? DueDate,
    string? AiRecommendation,
    decimal? AiConfidenceScore);
