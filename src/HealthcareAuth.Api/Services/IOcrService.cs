namespace HealthcareAuth.Api.Services;

public interface IOcrService
{
    Task<string> ExtractTextAsync(string filePath, CancellationToken cancellationToken = default);
}
