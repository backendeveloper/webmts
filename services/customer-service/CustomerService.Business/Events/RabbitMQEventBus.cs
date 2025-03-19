using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CustomerService.Business.Events;

public class RabbitMQEventBus : IEventBus, IDisposable
{
    private readonly ILogger<RabbitMQEventBus> _logger;
    private readonly RabbitMQSettings _settings;
    private IConnection _connection;
    private IModel _model;
    private bool _disposed;

    public RabbitMQEventBus(IOptions<RabbitMQSettings> options, ILogger<RabbitMQEventBus> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = options?.Value ?? throw new ArgumentNullException(nameof(options));
        InitializeRabbitMQ();
    }

    private async Task InitializeRabbitMQ()
    {
        if (_connection != null && _model != null)
            return;

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost,
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _model = _connection.CreateModel();

            _model.ExchangeDeclare(
                exchange: _settings.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            _logger.LogInformation("RabbitMQ connection initialized successfully to {Host}:{Port}",
                _settings.HostName, _settings.Port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing RabbitMQ connection to {Host}:{Port}",
                _settings.HostName, _settings.Port);
            throw;
        }
    }

    public async void Publish<T>(T @event) where T : IntegrationEvent
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(RabbitMQEventBus));

        var eventName = @event.GetType().Name.ToLowerInvariant();
        var routingKey = GetRoutingKeyFromEventName(eventName);

        _logger.LogInformation("Publishing event {EventName} with routing key {RoutingKey}", eventName, routingKey);

        try
        {
            var message = JsonSerializer.Serialize(@event, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var body = Encoding.UTF8.GetBytes(message);

            var properties = _model.CreateBasicProperties();
            properties.DeliveryMode = 2; // persistent
            properties.ContentType = "application/json";
            properties.MessageId = @event.Id.ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            _model.BasicPublish(
                exchange: _settings.ExchangeName,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Event {EventName} published successfully with routing key {RoutingKey}",
                eventName, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event {EventName} with routing key {RoutingKey}",
                eventName, routingKey);
            throw;
        }
    }

    private string GetRoutingKeyFromEventName(string eventName)
    {
        if (eventName.EndsWith("event"))
            eventName = eventName.Substring(0, eventName.Length - 5);

        if (eventName.Contains("Customer"))
            return $"customer.{eventName.Replace("Customer", "").ToLowerInvariant()}";

        if (eventName.Contains("Transaction"))
            return $"transaction.{eventName.Replace("Transaction", "").ToLowerInvariant()}";

        return eventName;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            _model?.Dispose();
            _connection?.Dispose();
            _disposed = true;
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Error during RabbitMQ connection dispose");
        }
    }
}