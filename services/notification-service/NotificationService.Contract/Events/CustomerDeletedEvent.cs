namespace NotificationService.Contract.Events;

public class CustomerDeletedEvent : IntegrationEvent
{
    public string CustomerId { get; set; }
}