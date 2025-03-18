using System.ComponentModel.DataAnnotations;
using MediatR;
using TransactionService.Contract.Responses;

namespace TransactionService.Contract.Requests;

public class CancelTransactionRequest : IRequest<CancelTransactionResponse>
{
    [Required]
    public Guid TransactionId { get; set; }
        
    [StringLength(500)]
    public string Reason { get; set; }
}