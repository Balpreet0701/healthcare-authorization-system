using HealthcareAuth.Api.Models;

namespace HealthcareAuth.Api.Services;

public interface IOllamaService
{
    Task<string> GenerateMedicalSummaryAsync(AuthorizationRequest request, CancellationToken cancellationToken = default);
    Task<RecommendationResult> GenerateRecommendationAsync(AuthorizationRequest request, CancellationToken cancellationToken = default);
}
