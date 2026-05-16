using Hangfire;
using HealthcareAuth.Api.Data;
using HealthcareAuth.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HealthcareAuth.Api.Services;

public class AuthorizationWorkflowService : IAuthorizationWorkflowService
{
    private readonly ApplicationDbContext _db;
    private readonly IOllamaService _ollamaService;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;
    private readonly IRabbitMqPublisher _rabbitMqPublisher;

    public AuthorizationWorkflowService(
        ApplicationDbContext db,
        IOllamaService ollamaService,
        INotificationService notificationService,
        IAuditService auditService,
        IRabbitMqPublisher rabbitMqPublisher)
    {
        _db = db;
        _ollamaService = ollamaService;
        _notificationService = notificationService;
        _auditService = auditService;
        _rabbitMqPublisher = rabbitMqPublisher;
    }

    public async Task SubmitAsync(int authorizationRequestId, string? userId, CancellationToken cancellationToken = default)
    {
        var request = await _db.AuthorizationRequests
            .Include(x => x.Patient)
            .FirstOrDefaultAsync(x => x.Id == authorizationRequestId, cancellationToken)
            ?? throw new KeyNotFoundException("Authorization request not found.");

        if (request.Status is not (AuthorizationStatus.Draft or AuthorizationStatus.PendingInformation))
        {
            throw new InvalidOperationException("Only draft or pending-information requests can be submitted.");
        }

        var from = request.Status;
        request.Status = AuthorizationStatus.Submitted;
        request.SubmittedAt ??= DateTime.UtcNow;
        request.LastUpdatedAt = DateTime.UtcNow;
        request.StatusHistory.Add(new AuthorizationStatusHistory
        {
            FromStatus = from,
            ToStatus = AuthorizationStatus.Submitted,
            ChangedById = userId,
            Reason = "Submitted for review"
        });

        await _db.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("Submit", nameof(AuthorizationRequest), request.Id.ToString(), $"Submitted {request.RequestNumber}", cancellationToken);
        await _notificationService.NotifyRoleAsync(AppRoles.Reviewer, "New authorization submitted", $"{request.RequestNumber} is ready for review.", $"/authorizations/{request.Id}", cancellationToken);
        await _rabbitMqPublisher.PublishAsync("authorization.submitted", new { request.Id, request.RequestNumber }, cancellationToken);

        BackgroundJob.Enqueue<IBackgroundProcessingService>(service => service.AnalyzeAuthorizationAsync(request.Id));
    }

    public async Task GenerateAiInsightsAsync(int authorizationRequestId, CancellationToken cancellationToken = default)
    {
        var request = await _db.AuthorizationRequests
            .Include(x => x.Patient)
            .Include(x => x.Documents)
            .FirstOrDefaultAsync(x => x.Id == authorizationRequestId, cancellationToken);

        if (request is null)
        {
            return;
        }

        var summary = await _ollamaService.GenerateMedicalSummaryAsync(request, cancellationToken);
        var recommendation = await _ollamaService.GenerateRecommendationAsync(request, cancellationToken);

        request.AiSummary = summary;
        request.AiRecommendation = recommendation.Decision;
        request.AiConfidenceScore = Math.Round(recommendation.Confidence * 100, 2);
        request.AiRationale = recommendation.Rationale;

        if (request.Status == AuthorizationStatus.Submitted)
        {
            request.StatusHistory.Add(new AuthorizationStatusHistory
            {
                FromStatus = AuthorizationStatus.Submitted,
                ToStatus = AuthorizationStatus.InReview,
                Reason = "AI insight generation complete"
            });
            request.Status = AuthorizationStatus.InReview;
        }

        request.LastUpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await _rabbitMqPublisher.PublishAsync("authorization.ai.completed", new
        {
            request.Id,
            request.RequestNumber,
            request.AiRecommendation,
            request.AiConfidenceScore
        }, cancellationToken);
    }

    public async Task ReviewAsync(int authorizationRequestId, AuthorizationStatus decision, string reason, string? reviewerId, CancellationToken cancellationToken = default)
    {
        if (decision is not (AuthorizationStatus.Approved or AuthorizationStatus.Denied or AuthorizationStatus.PendingInformation))
        {
            throw new InvalidOperationException("Review decision must be Approved, Denied, or PendingInformation.");
        }

        var request = await _db.AuthorizationRequests
            .FirstOrDefaultAsync(x => x.Id == authorizationRequestId, cancellationToken)
            ?? throw new KeyNotFoundException("Authorization request not found.");

        var from = request.Status;
        request.Status = decision;
        request.AssignedReviewerId = reviewerId ?? request.AssignedReviewerId;
        request.DecisionReason = reason;
        request.LastUpdatedAt = DateTime.UtcNow;
        request.StatusHistory.Add(new AuthorizationStatusHistory
        {
            FromStatus = from,
            ToStatus = decision,
            ChangedById = reviewerId,
            Reason = reason
        });

        await _db.SaveChangesAsync(cancellationToken);
        await _auditService.WriteAsync("ReviewDecision", nameof(AuthorizationRequest), request.Id.ToString(), $"{decision}: {reason}", cancellationToken);
        await _rabbitMqPublisher.PublishAsync("authorization.reviewed", new { request.Id, request.RequestNumber, decision, reason }, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.CreatedById))
        {
            await _notificationService.NotifyUserAsync(
                request.CreatedById,
                "Authorization decision updated",
                $"{request.RequestNumber} moved to {decision}.",
                $"/authorizations/{request.Id}",
                cancellationToken);
        }
    }
}
