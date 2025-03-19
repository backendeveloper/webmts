namespace CustomerService.Business.Events;

public class CustomerUpdatedEvent : IntegrationEvent
{
    public string CustomerId { get; set; }
    public string CustomerName { get; set; }
    public string CustomerEmail { get; set; }
}