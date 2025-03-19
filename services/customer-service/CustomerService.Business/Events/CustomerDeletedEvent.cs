namespace CustomerService.Business.Events;

public class CustomerDeletedEvent : IntegrationEvent
{
    public string CustomerId { get; set; }
}