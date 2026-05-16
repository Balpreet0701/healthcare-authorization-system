namespace HealthcareAuth.Api.Services;

public interface IRabbitMqPublisher
{
    Task PublishAsync(string eventType, object payload, CancellationToken cancellationToken = default);
}
