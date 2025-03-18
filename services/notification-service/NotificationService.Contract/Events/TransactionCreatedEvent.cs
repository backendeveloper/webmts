namespace NotificationService.Contract.Events;

public class TransactionCreatedEvent : IntegrationEvent
{
    public string TransactionId { get; set; }
    public string TransactionNumber { get; set; }
    public string Type { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string CustomerId { get; set; }
    public string CustomerName { get; set; }
    public string CustomerEmail { get; set; }
}