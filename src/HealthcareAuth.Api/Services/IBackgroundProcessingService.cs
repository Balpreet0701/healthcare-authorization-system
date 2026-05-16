namespace HealthcareAuth.Api.Services;

public interface IBackgroundProcessingService
{
    Task ProcessDocumentAsync(int documentId);
    Task AnalyzeAuthorizationAsync(int authorizationRequestId);
}
