using HealthcareAuth.Api.Models;

namespace HealthcareAuth.Api.Contracts;

public static class MappingExtensions
{
    public static PatientResponse ToResponse(this Patient patient)
    {
        return new PatientResponse(
            patient.Id,
            patient.MedicalRecordNumber,
            patient.FirstName,
            patient.LastName,
            patient.DateOfBirth,
            patient.Gender,
            patient.Phone,
            patient.Email,
            patient.InsuranceProvider,
            patient.MemberNumber,
            patient.CreatedAt,
            patient.UpdatedAt);
    }

    public static AuthorizationListItemResponse ToListItem(this AuthorizationRequest request)
    {
        return new AuthorizationListItemResponse(
            request.Id,
            request.RequestNumber,
            request.Patient is null ? "Unknown patient" : $"{request.Patient.FirstName} {request.Patient.LastName}",
            request.RequestedService,
            request.DiagnosisCode,
            request.ProcedureCode,
            request.Priority,
            request.Status,
            request.CreatedAt,
            request.DueDate,
            request.AiRecommendation,
            request.AiConfidenceScore);
    }

    public static AuthorizationResponse ToResponse(this AuthorizationRequest request)
    {
        return new AuthorizationResponse(
            request.Id,
            request.RequestNumber,
            request.PatientId,
            request.Patient is null ? "Unknown patient" : $"{request.Patient.FirstName} {request.Patient.LastName}",
            request.Patient?.MedicalRecordNumber ?? string.Empty,
            request.RequestedService,
            request.DiagnosisCode,
            request.ProcedureCode,
            request.Priority,
            request.Status,
            request.ClinicalNotes,
            request.CreatedAt,
            request.SubmittedAt,
            request.DueDate,
            request.AiSummary,
            request.AiRecommendation,
            request.AiConfidenceScore,
            request.AiRationale,
            request.DecisionReason,
            request.Documents
                .OrderByDescending(x => x.UploadedAt)
                .Select(x => new MedicalDocumentResponse(
                    x.Id,
                    x.FileName,
                    x.ContentType,
                    x.FileSize,
                    x.OcrStatus,
                    x.OcrText,
                    x.OcrError,
                    x.UploadedAt))
                .ToList(),
            request.UrlAttachments
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new UrlAttachmentResponse(
                    x.Id,
                    x.Title,
                    x.Url,
                    x.Description,
                    x.CreatedAt))
                .ToList(),
            request.StatusHistory
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new StatusHistoryResponse(
                    x.FromStatus,
                    x.ToStatus,
                    x.Reason,
                    x.CreatedAt))
                .ToList());
    }
}
