using System.Text;
using System.Text.Json;
using HealthcareAuth.Api.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace HealthcareAuth.Api.Services;

public class RabbitMqPublisher : IRabbitMqPublisher
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(IOptions<RabbitMqOptions> options, ILogger<RabbitMqPublisher> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task PublishAsync(string eventType, object payload, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("RabbitMQ disabled. Event {EventType} was not published.", eventType);
            return Task.CompletedTask;
        }

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Fanout, durable: true);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                eventType,
                occurredAt = DateTime.UtcNow,
                payload
            }));

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.Type = eventType;

            channel.BasicPublish(_options.ExchangeName, routingKey: string.Empty, basicProperties: properties, body: body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RabbitMQ publish failed for {EventType}", eventType);
        }

        return Task.CompletedTask;
    }
}
