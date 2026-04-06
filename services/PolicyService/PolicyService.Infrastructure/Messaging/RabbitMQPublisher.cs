using RabbitMQ.Client;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace PolicyService.Infrastructure.Messaging;

public class RabbitMQPublisher
{
    private readonly ILogger<RabbitMQPublisher> _logger;

    public RabbitMQPublisher(ILogger<RabbitMQPublisher> logger)
    {
        _logger = logger;
    }

    public void PublishPolicyCreated(int policyId, int userId, string policyNumber)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = "localhost"
            };

            using var connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            using var channel = connection.CreateChannelAsync().GetAwaiter().GetResult();

            channel.ExchangeDeclareAsync(exchange: "smartsure", type: ExchangeType.Direct, durable: true)
                .GetAwaiter().GetResult();
            channel.QueueDeclareAsync(queue: "policy.created", durable: true, exclusive: false, autoDelete: false)
                .GetAwaiter().GetResult();
            channel.QueueBindAsync(queue: "policy.created", exchange: "smartsure", routingKey: "policy.created")
                .GetAwaiter().GetResult();

            var payload = new
            {
                policyId,
                userId,
                policyNumber,
                timestamp = DateTime.UtcNow
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
            channel.BasicPublishAsync(exchange: "smartsure", routingKey: "policy.created", body: body)
                .GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RabbitMQ is not available. Skipping policy.created publish.");
        }
    }
}
