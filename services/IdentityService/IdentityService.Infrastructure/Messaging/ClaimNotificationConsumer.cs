using System.Text;
using System.Text.Json;
using IdentityService.Application.DTOs;
using IdentityService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace IdentityService.Infrastructure.Messaging;

public class ClaimNotificationConsumer : BackgroundService
{
    private const string QueueName = "claim.status.notification";

    private readonly ILogger<ClaimNotificationConsumer> _logger;
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;

    private IConnection? _connection;
    private IChannel?    _channel;

    public ClaimNotificationConsumer(
        ILogger<ClaimNotificationConsumer> logger,
        IConfiguration config,
        IServiceScopeFactory scopeFactory)
    {
        _logger      = logger;
        _config      = config;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndConsumeAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "RabbitMQ connection lost. Retrying in 5 seconds...");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task ConnectAndConsumeAsync(CancellationToken stoppingToken)
    {
        var rmq = _config.GetSection("RabbitMQ");
        var factory = new ConnectionFactory
        {
            HostName = rmq["Host"]     ?? "localhost",
            UserName = rmq["Username"] ?? "guest",
            Password = rmq["Password"] ?? "guest"
        };

        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel    = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue:      QueueName,
            durable:    true,
            exclusive:  false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json         = Encoding.UTF8.GetString(ea.Body.ToArray());
                var notification = JsonSerializer.Deserialize<ClaimStatusNotificationDto>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (notification is null)
                {
                    _logger.LogWarning("Received null claim notification message — skipping");
                    await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                    return;
                }

                _logger.LogInformation(
                    "Received claim status notification: {ClaimNumber} → {NewStatus} for {Email}",
                    notification.ClaimNumber, notification.NewStatus, notification.CustomerEmail);

                using var scope        = _scopeFactory.CreateScope();
                var       emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                await emailService.SendClaimStatusEmailAsync(notification);

                _logger.LogInformation(
                    "Claim status email sent to {Email} for claim {ClaimNumber}",
                    notification.CustomerEmail, notification.ClaimNumber);

                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process claim notification message — nacking");
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        await _channel.BasicConsumeAsync(
            queue:       QueueName,
            autoAck:     false,
            consumerTag: "identity-claim-notification",
            noLocal:     false,
            exclusive:   false,
            arguments:   null,
            consumer:    consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation(
            "ClaimNotificationConsumer started — listening on queue '{Queue}' at {Host}",
            QueueName, factory.HostName);

        // Keep the consumer alive until cancellation
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel    != null) await _channel.CloseAsync(cancellationToken: cancellationToken);
        if (_connection != null) await _connection.CloseAsync(cancellationToken: cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
