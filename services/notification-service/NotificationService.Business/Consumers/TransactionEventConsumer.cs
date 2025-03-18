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

public class TransactionEventConsumer : BaseEventConsumer
    {
        public TransactionEventConsumer(
            IOptions<RabbitMQSettings> options, 
            IServiceScopeFactory serviceScopeFactory,
            ILogger<TransactionEventConsumer> logger) 
            : base(options, serviceScopeFactory, logger)
        {
            // Queue'yu oluştur
            _queueName = $"{_rabbitMQSettings.QueueName}.transaction";
            
            _channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);
            
            // Exchange'e bağla (routing key olarak transaction.* kullan)
            _channel.QueueBind(
                queue: _queueName,
                exchange: _rabbitMQSettings.ExchangeName,
                routingKey: "transaction.*");
            
            _logger.LogInformation($"Transaction event consumer initialized, queue: {_queueName}");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Transaction event consumer is starting");
            
            stoppingToken.Register(() => 
                _logger.LogInformation("Transaction event consumer is stopping"));

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
                    if (routingKey == "transaction.created")
                    {
                        await ProcessEventAsync<TransactionCreatedEvent>(message);
                    }
                    else if (routingKey == "transaction.status-changed")
                    {
                        await ProcessEventAsync<TransactionStatusChangedEvent>(message);
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
            if (@event is TransactionCreatedEvent transactionCreatedEvent)
            {
                await HandleTransactionCreatedEventAsync(transactionCreatedEvent, mediator);
            }
            else if (@event is TransactionStatusChangedEvent transactionStatusChangedEvent)
            {
                await HandleTransactionStatusChangedEventAsync(transactionStatusChangedEvent, mediator);
            }
        }

        private async Task HandleTransactionCreatedEventAsync(TransactionCreatedEvent @event, IMediator mediator)
        {
            _logger.LogInformation($"Handling TransactionCreatedEvent for transaction {@@event.TransactionId}");
            
            // Şablon verilerini hazırla
            var templateData = new Dictionary<string, string>
            {
                { "TransactionId", @event.TransactionId },
                { "TransactionNumber", @event.TransactionNumber },
                { "Amount", @event.Amount.ToString("N2") },
                { "Currency", @event.Currency },
                { "CustomerName", @event.CustomerName }
            };
            
            // Bildirim isteği oluştur
            var notificationCommand = new SendNotificationCommand
            {
                Type = NotificationType.Email,
                TemplateName = "TransactionCreated",
                RecipientId = @event.CustomerId,
                RecipientInfo = @event.CustomerEmail,
                TemplateData = templateData,
                RelatedEntityId = @event.TransactionId,
                RelatedEntityType = "Transaction"
            };
            
            // Bildirimi gönder
            await mediator.Send(notificationCommand);
        }

        private async Task HandleTransactionStatusChangedEventAsync(TransactionStatusChangedEvent @event, IMediator mediator)
        {
            _logger.LogInformation($"Handling TransactionStatusChangedEvent for transaction {@@event.TransactionId}, new status: {@@event.NewStatus}");
            
            // İlgili şablon adını belirle
            string templateName = null;
            if (@event.NewStatus == "Completed")
            {
                templateName = "TransactionCompleted";
            }
            else if (@event.NewStatus == "Failed")
            {
                templateName = "TransactionFailed";
            }
            
            // Şablon adı belirlendiyse bildirim gönder
            if (!string.IsNullOrEmpty(templateName))
            {
                // Şablon verilerini hazırla
                var templateData = new Dictionary<string, string>
                {
                    { "TransactionId", @event.TransactionId },
                    { "TransactionNumber", @event.TransactionNumber },
                    { "OldStatus", @event.OldStatus },
                    { "NewStatus", @event.NewStatus },
                    { "Amount", @event.Amount.ToString("N2") },
                    { "Currency", @event.Currency },
                    { "CustomerName", @event.CustomerName }
                };
                
                // Bildirim isteği oluştur
                var notificationCommand = new SendNotificationCommand
                {
                    Type = NotificationType.Email,
                    TemplateName = templateName,
                    RecipientId = @event.CustomerId,
                    RecipientInfo = @event.CustomerEmail,
                    TemplateData = templateData,
                    RelatedEntityId = @event.TransactionId,
                    RelatedEntityType = "Transaction"
                };
                
                // Bildirimi gönder
                await mediator.Send(notificationCommand);
            }
        }
    }