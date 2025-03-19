using System.Text;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Common.Configuration;
using NotificationService.Contract.Enums;
using NotificationService.Contract.Events;
using NotificationService.Contract.Requests;
using RabbitMQ.Client.Events;

namespace NotificationService.Business.Consumers;

public class TransactionEventConsumer : BaseEventConsumer
{
    public TransactionEventConsumer(
        IOptions<RabbitMQSettings> options,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<TransactionEventConsumer> logger)
        : base(options, serviceScopeFactory, logger)
    {
        _queueName = $"{_rabbitMQSettings.QueueName}.transaction";

        _model.QueueDeclare(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false, null);

        _model.QueueBind(
            queue: _queueName,
            exchange: _rabbitMQSettings.ExchangeName,
            routingKey: "transaction.*", null);

        _logger.LogInformation($"Transaction event consumer initialized, queue: {_queueName}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Transaction event consumer is starting");

        stoppingToken.Register(() =>
            _logger.LogInformation("Transaction event consumer is stopping"));

        var consumer = new AsyncEventingBasicConsumer(_model);
        consumer.Received += async (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var routingKey = ea.RoutingKey;

            _logger.LogInformation($"Received message with routing key: {routingKey}");

            try
            {
                if (routingKey == "transaction.created")
                    await ProcessEventAsync<TransactionCreatedEvent>(message);
                else if (routingKey == "transaction.status-changed")
                    await ProcessEventAsync<TransactionStatusChangedEvent>(message);

                _model.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing message with routing key: {routingKey}");
                _model.BasicAck(ea.DeliveryTag, false);
            }
        };

        _model.BasicConsume(_queueName, false, null, false, false, null, consumer);

        while (!stoppingToken.IsCancellationRequested)
            await Task.Delay(1000, stoppingToken);
    }

    protected override async Task HandleEventAsync<TEvent>(TEvent @event, IMediator mediator)
    {
        if (@event is TransactionCreatedEvent transactionCreatedEvent)
            await HandleTransactionCreatedEventAsync(transactionCreatedEvent, mediator);
        else if (@event is TransactionStatusChangedEvent transactionStatusChangedEvent)
            await HandleTransactionStatusChangedEventAsync(transactionStatusChangedEvent, mediator);
    }

    private async Task HandleTransactionCreatedEventAsync(TransactionCreatedEvent @event, IMediator mediator)
    {
        _logger.LogInformation($"Handling TransactionCreatedEvent for transaction {@event.TransactionId}");

        var templateData = new Dictionary<string, string>
        {
            { "TransactionId", @event.TransactionId },
            { "TransactionNumber", @event.TransactionNumber },
            { "Amount", @event.Amount.ToString("N2") },
            { "Currency", @event.Currency },
            { "CustomerName", @event.CustomerName }
        };

        var notificationCommand = new SendNotificationRequest
        {
            Type = NotificationType.Email,
            TemplateName = "TransactionCreated",
            RecipientId = @event.CustomerId,
            RecipientInfo = @event.CustomerEmail,
            TemplateData = templateData,
            RelatedEntityId = @event.TransactionId,
            RelatedEntityType = "Transaction"
        };

        await mediator.Send(notificationCommand);
    }

    private async Task HandleTransactionStatusChangedEventAsync(TransactionStatusChangedEvent @event,
        IMediator mediator)
    {
        _logger.LogInformation(
            $"Handling TransactionStatusChangedEvent for transaction {@event.TransactionId}, new status: {@event.NewStatus}");

        string templateName = null;
        if (@event.NewStatus == "Completed")
            templateName = "TransactionCompleted";
        else if (@event.NewStatus == "Failed")
            templateName = "TransactionFailed";

        if (!string.IsNullOrEmpty(templateName))
        {
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

            var notificationCommand = new SendNotificationRequest
            {
                Type = NotificationType.Email,
                TemplateName = templateName,
                RecipientId = @event.CustomerId,
                RecipientInfo = @event.CustomerEmail,
                TemplateData = templateData,
                RelatedEntityId = @event.TransactionId,
                RelatedEntityType = "Transaction"
            };

            await mediator.Send(notificationCommand);
        }
    }
}