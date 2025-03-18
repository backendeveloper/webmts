using System.Text;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Common.Configuration;
using NotificationService.Contract.Enums;
using NotificationService.Contract.Events;
using NotificationService.Contract.Requests;

namespace NotificationService.Business.Consumers;

public class CustomerEventConsumer : BaseEventConsumer
    {
        public CustomerEventConsumer(
            IOptions<RabbitMQSettings> options, 
            IServiceScopeFactory serviceScopeFactory,
            ILogger<CustomerEventConsumer> logger) 
            : base(options, serviceScopeFactory, logger)
        {
            // Queue'yu oluştur
            _queueName = $"{_rabbitMQSettings.QueueName}.customer";
            
            _channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);
            
            // Exchange'e bağla (routing key olarak customer.* kullan)
            _channel.QueueBind(
                queue: _queueName,
                exchange: _rabbitMQSettings.ExchangeName,
                routingKey: "customer.*");
            
            _logger.LogInformation($"Customer event consumer initialized, queue: {_queueName}");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Customer event consumer is starting");
            
            stoppingToken.Register(() => 
                _logger.LogInformation("Customer event consumer is stopping"));

            var consumer = new EventingBasicConsumer(_channel);
            
            consumer.Received += async (_, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var routingKey = ea.RoutingKey;
                
                _logger.LogInformation($"Received message with routing key: {routingKey}");
                
                try
                {
                    // Routing key'e göre uygun event handler'ı seç
                    if (routingKey == "customer.created")
                    {
                        await ProcessEventAsync<CustomerCreatedEvent>(message);
                    }
                    
                    // İşlem başarılı, mesajı onayla
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing message with routing key: {routingKey}");
                    
                    // İşlem başarısız, mesajı reddet (requeue: true ile yeniden kuyruğa eklenir)
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };
            
            _channel.BasicConsume(
                queue: _queueName,
                autoAck: false,
                consumer: consumer);
            
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        protected override async Task HandleEventAsync<TEvent>(TEvent @event, IMediator mediator)
        {
            if (@event is CustomerCreatedEvent customerCreatedEvent)
            {
                await HandleCustomerCreatedEventAsync(customerCreatedEvent, mediator);
            }
        }

        private async Task HandleCustomerCreatedEventAsync(CustomerCreatedEvent @event, IMediator mediator)
        {
            _logger.LogInformation($"Handling CustomerCreatedEvent for customer {@@event.CustomerId}");
            
            // Şablon verilerini hazırla
            var templateData = new Dictionary<string, string>
            {
                { "CustomerId", @event.CustomerId },
                { "CustomerName", @event.CustomerName }
            };
            
            // Bildirim isteği oluştur
            var notificationCommand = new SendNotificationRequest
            {
                Type = NotificationType.Email,
                TemplateName = "NewCustomer",
                RecipientId = @event.CustomerId,
                RecipientInfo = @event.CustomerEmail,
                TemplateData = templateData,
                RelatedEntityId = @event.CustomerId,
                RelatedEntityType = "Customer"
            };
            
            // Bildirimi gönder
            await mediator.Send(notificationCommand);
        }
    }