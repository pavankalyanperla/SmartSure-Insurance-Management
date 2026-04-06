using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace ClaimsService.Infrastructure.Messaging;

public class RabbitMQPublisher
{
    private readonly ILogger<RabbitMQPublisher> _logger;

    public RabbitMQPublisher(ILogger<RabbitMQPublisher> logger)
    {
        _logger = logger;
    }

    public void PublishClaimSubmitted(int claimId, int customerId, string claimNumber)
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
            channel.QueueDeclareAsync(queue: "claim.submitted", durable: true, exclusive: false, autoDelete: false)
                .GetAwaiter().GetResult();
            channel.QueueBindAsync(queue: "claim.submitted", exchange: "smartsure", routingKey: "claim.submitted")
                .GetAwaiter().GetResult();

            var payload = new
            {
                claimId,
                customerId,
                claimNumber,
                timestamp = DateTime.UtcNow
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
            channel.BasicPublishAsync(exchange: "smartsure", routingKey: "claim.submitted", body: body)
                .GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RabbitMQ is not available. Skipping claim.submitted publish.");
        }
    }
}
