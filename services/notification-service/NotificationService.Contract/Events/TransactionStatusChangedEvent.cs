namespace NotificationService.Contract.Events;

public class TransactionStatusChangedEvent : IntegrationEvent
{
    public string TransactionId { get; set; }
    public string TransactionNumber { get; set; }
    public string OldStatus { get; set; }
    public string NewStatus { get; set; }
    public string CustomerId { get; set; }
    public string CustomerName { get; set; }
    public string CustomerEmail { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
}