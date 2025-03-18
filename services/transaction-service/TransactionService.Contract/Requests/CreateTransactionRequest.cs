using System.ComponentModel.DataAnnotations;
using MediatR;
using TransactionService.Contract.Enums;
using TransactionService.Contract.Responses;

namespace TransactionService.Contract.Requests;

public class CreateTransactionRequest : IRequest<CreateTransactionResponse>
{
    [Required]
    public TransactionType Type { get; set; }
        
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero")]
    public decimal Amount { get; set; }
        
    [Required]
    [StringLength(10)]
    public string Currency { get; set; }
        
    [Required]
    [StringLength(50)]
    public string SourceAccountId { get; set; }
        
    [Required]
    [StringLength(50)]
    public string DestinationAccountId { get; set; }
        
    [StringLength(500)]
    public string Description { get; set; }
        
    public Guid? CustomerId { get; set; }
}