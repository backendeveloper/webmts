using System.ComponentModel.DataAnnotations;
using TransactionService.Contract.Enums;

namespace TransactionService.Data.Entities;

public class Transaction : BaseEntity
{
    [Key] public Guid Id { get; set; }

    public string TransactionNumber { get; set; }

    public TransactionType Type { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; }

    public TransactionStatusType Status { get; set; }

    public string SourceAccountId { get; set; }

    public string DestinationAccountId { get; set; }

    public string Description { get; set; }

    public Guid? CustomerId { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string CreatedBy { get; set; }

    public string UpdatedBy { get; set; }

    public ICollection<TransactionHistory> TransactionHistories { get; set; } = new List<TransactionHistory>();
}