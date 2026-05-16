using HealthcareAuth.Api.Models;

namespace HealthcareAuth.Api.Services;

public interface IAuthorizationWorkflowService
{
    Task SubmitAsync(int authorizationRequestId, string? userId, CancellationToken cancellationToken = default);
    Task GenerateAiInsightsAsync(int authorizationRequestId, CancellationToken cancellationToken = default);
    Task ReviewAsync(int authorizationRequestId, AuthorizationStatus decision, string reason, string? reviewerId, CancellationToken cancellationToken = default);
}
