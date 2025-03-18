using System.ComponentModel.DataAnnotations;
using TransactionService.Contract.Enums;

namespace TransactionService.Data.Entities;

public class TransactionHistory
{
    [Key] public Guid Id { get; set; }

    public Guid TransactionId { get; set; }

    public TransactionStatusType OldStatus { get; set; }

    public TransactionStatusType NewStatus { get; set; }

    public string Notes { get; set; }

    public string CreatedBy { get; set; }

    public Transaction Transaction { get; set; }
}