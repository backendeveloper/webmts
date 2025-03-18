using TransactionService.Contract.Enums;

namespace TransactionService.Contract.Dtos;

public class TransactionDto
{
    public Guid Id { get; set; }
    public string TransactionNumber { get; set; }
    public string Type { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public TransactionStatusType Status { get; set; }
    public string SourceAccountId { get; set; }
    public string DestinationAccountId { get; set; }
    public string Description { get; set; }
    public Guid? CustomerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}