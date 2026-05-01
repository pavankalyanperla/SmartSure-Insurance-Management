using System.Text;
using System.Text.Json;
using AdminService.Application.DTOs;
using AdminService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace AdminService.Infrastructure.Services;

public class NotificationPublisher : INotificationPublisher
{
    private const string QueueName = "claim.status.notification";

    private readonly ILogger<NotificationPublisher> _logger;
    private readonly IConfiguration _config;

    public NotificationPublisher(ILogger<NotificationPublisher> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task PublishClaimStatusChangedAsync(ClaimStatusNotificationDto notification)
    {
        try
        {
            var rmq = _config.GetSection("RabbitMQ");
            var factory = new ConnectionFactory
            {
                HostName = rmq["Host"]     ?? "localhost",
                UserName = rmq["Username"] ?? "guest",
                Password = rmq["Password"] ?? "guest"
            };

            using var connection = await factory.CreateConnectionAsync();
            using var channel    = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue:      QueueName,
                durable:    true,
                exclusive:  false,
                autoDelete: false);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(notification));

            var props = new BasicProperties { Persistent = true };
            await channel.BasicPublishAsync(
                exchange:   string.Empty,
                routingKey: QueueName,
                mandatory:  false,
                basicProperties: props,
                body:       body);

            _logger.LogInformation(
                "Published claim status notification for claim {ClaimNumber} → {NewStatus}",
                notification.ClaimNumber, notification.NewStatus);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "RabbitMQ unavailable — skipping claim status notification for claim {ClaimId}",
                notification.ClaimId);
        }
    }
}
