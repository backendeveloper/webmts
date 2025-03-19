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

public class CustomerEventConsumer : BaseEventConsumer
{
    public CustomerEventConsumer(
        IOptions<RabbitMQSettings> options,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<CustomerEventConsumer> logger)
        : base(options, serviceScopeFactory, logger)
    {
        _queueName = $"{_rabbitMQSettings.QueueName}.customer";

        _model.QueueDeclare(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false, null);

        _model.QueueBind(
            queue: _queueName,
            exchange: _rabbitMQSettings.ExchangeName,
            routingKey: "customer.*", null);
        ;

        _logger.LogInformation($"Customer event consumer initialized, queue: {_queueName}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Customer event consumer is starting");

        stoppingToken.Register(() =>
            _logger.LogInformation("Customer event consumer is stopping"));

        var consumer = new AsyncEventingBasicConsumer(_model);
        consumer.Received += async (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var routingKey = ea.RoutingKey;

            _logger.LogInformation($"Received message with routing key: {routingKey}");

            try
            {
                if (routingKey == "customer.created")
                    await ProcessEventAsync<CustomerCreatedEvent>(message);
                else if (routingKey == "customer.updated")
                    await ProcessEventAsync<CustomerUpdatedEvent>(message);
                else if (routingKey == "customer.deleted")
                    await ProcessEventAsync<CustomerDeletedEvent>(message);

                _model.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing message with routing key: {routingKey}");
                _model.BasicAck(ea.DeliveryTag, false);
            }
        };

        _model.BasicConsume(_queueName, false, "", false, false, null, consumer);

        while (!stoppingToken.IsCancellationRequested)
            await Task.Delay(1000, stoppingToken);
    }

    protected override async Task HandleEventAsync<TEvent>(TEvent @event, IMediator mediator)
    {
        if (@event is CustomerCreatedEvent customerCreatedEvent)
            await HandleCustomerCreatedEventAsync(customerCreatedEvent, mediator);
        else if (@event is CustomerUpdatedEvent customerUpdatedEvent)
            await HandleCustomerUpdatedEventAsync(customerUpdatedEvent, mediator);
        else if (@event is CustomerDeletedEvent customerDeletedEvent)
            await HandleCustomerDeletedEventAsync(customerDeletedEvent, mediator);
    }

    private async Task HandleCustomerCreatedEventAsync(CustomerCreatedEvent @event, IMediator mediator)
    {
        _logger.LogInformation($"Handling CustomerCreatedEvent for customer {@event.CustomerId}");

        var templateData = new Dictionary<string, string>
        {
            { "CustomerId", @event.CustomerId },
            { "CustomerName", @event.CustomerName }
        };

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

        await mediator.Send(notificationCommand);
    }

    private async Task HandleCustomerUpdatedEventAsync(CustomerUpdatedEvent @event, IMediator mediator)
    {
        _logger.LogInformation($"Handling CustomerUpdatedEvent for customer {@event.CustomerId}");
        _logger.LogInformation("Customer updated: {CustomerId}, {CustomerName}, {CustomerEmail}",
            @event.CustomerId, @event.CustomerName, @event.CustomerEmail);
    }

    private async Task HandleCustomerDeletedEventAsync(CustomerDeletedEvent @event, IMediator mediator)
    {
        _logger.LogInformation($"Handling CustomerDeletedEvent for customer {@event.CustomerId}");
        _logger.LogInformation("Customer deleted: {CustomerId}", @event.CustomerId);
    }
}