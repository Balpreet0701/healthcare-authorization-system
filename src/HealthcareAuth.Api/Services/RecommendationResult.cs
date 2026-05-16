namespace HealthcareAuth.Api.Services;

public record RecommendationResult(
    string Decision,
    decimal Confidence,
    string Rationale);
