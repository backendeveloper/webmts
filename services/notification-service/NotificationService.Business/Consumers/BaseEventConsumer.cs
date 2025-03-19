using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Common.Configuration;
using NotificationService.Contract.Events;
using RabbitMQ.Client;

namespace NotificationService.Business.Consumers;

public abstract class BaseEventConsumer : BackgroundService
    {
        protected readonly IServiceScopeFactory _serviceScopeFactory;
        protected readonly ILogger<BaseEventConsumer> _logger;
        protected readonly RabbitMQSettings _rabbitMQSettings;
        protected IConnection _connection;
        protected IChannel _channel;
        protected string _queueName;

        public BaseEventConsumer(
            IOptions<RabbitMQSettings> options, 
            IServiceScopeFactory serviceScopeFactory,
            ILogger<BaseEventConsumer> logger)
        {
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _rabbitMQSettings = options?.Value ?? throw new ArgumentNullException(nameof(options));
            
            InitializeRabbitMQConnection().GetAwaiter().GetResult();
        }

        protected async Task InitializeRabbitMQConnection()
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = _rabbitMQSettings.HostName,
                    Port = _rabbitMQSettings.Port,
                    UserName = _rabbitMQSettings.UserName,
                    Password = _rabbitMQSettings.Password,
                    VirtualHost = _rabbitMQSettings.VirtualHost ?? "/"
                };

                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();
                await _channel.ExchangeDeclareAsync(
                    exchange: _rabbitMQSettings.ExchangeName, 
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing RabbitMQ connection");
                throw;
            }
        }

        protected async Task ProcessEventAsync<TEvent>(string message) where TEvent : IntegrationEvent
        {
            try
            {
                _logger.LogInformation($"Processing event: {typeof(TEvent).Name}");
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var @event = JsonSerializer.Deserialize<TEvent>(message, options);
                if (@event == null)
                {
                    _logger.LogWarning($"Could not deserialize event: {typeof(TEvent).Name}");
                    return;
                }
                
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    await HandleEventAsync(@event, mediator);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing event: {typeof(TEvent).Name}");
            }
        }

        protected abstract Task HandleEventAsync<TEvent>(TEvent @event, IMediator mediator) where TEvent : IntegrationEvent;

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }