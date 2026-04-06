namespace AdminService.Infrastructure.Messaging;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

public class RabbitMQConsumer : BackgroundService
{
    private readonly ILogger<RabbitMQConsumer> _logger;
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMQConsumer(ILogger<RabbitMQConsumer> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            // Setup exchanges and queues
            await _channel.ExchangeDeclareAsync("smartsure", ExchangeType.Direct, durable: true, cancellationToken: stoppingToken);
            await _channel.QueueDeclareAsync("claim.submitted", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
            await _channel.QueueDeclareAsync("policy.created", durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

            await _channel.QueueBindAsync("claim.submitted", "smartsure", "claim.submitted", cancellationToken: stoppingToken);
            await _channel.QueueBindAsync("policy.created", "smartsure", "policy.created", cancellationToken: stoppingToken);

            // Setup consumers
            var claimConsumer = new AsyncEventingBasicConsumer(_channel);
            claimConsumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    _logger.LogInformation("New claim submitted: {Message}", message);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error processing claim message: {Message}", ex.Message);
                }
            };

            var policyConsumer = new AsyncEventingBasicConsumer(_channel);
            policyConsumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    _logger.LogInformation("New policy created: {Message}", message);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error processing policy message: {Message}", ex.Message);
                }
            };

            await _channel.BasicConsumeAsync("claim.submitted", autoAck: true, consumerTag: "claim-consumer", noLocal: false, exclusive: false, arguments: null, consumer: claimConsumer, cancellationToken: stoppingToken);
            await _channel.BasicConsumeAsync("policy.created", autoAck: true, consumerTag: "policy-consumer", noLocal: false, exclusive: false, arguments: null, consumer: policyConsumer, cancellationToken: stoppingToken);

            _logger.LogInformation("RabbitMQ consumer started");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("RabbitMQ consumer error: {Message}. Service will continue without messaging.", ex.Message);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
        {
            await _channel.CloseAsync(cancellationToken: cancellationToken);
        }
        if (_connection != null)
        {
            await _connection.CloseAsync(cancellationToken: cancellationToken);
        }
        await base.StopAsync(cancellationToken);
    }
}
