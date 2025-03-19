namespace CustomerService.Business.Events;

public class CustomerCreatedEvent : IntegrationEvent
{
    public string CustomerId { get; set; }
    public string CustomerName { get; set; }
    public string CustomerEmail { get; set; }
    public string CustomerPhone { get; set; }
}