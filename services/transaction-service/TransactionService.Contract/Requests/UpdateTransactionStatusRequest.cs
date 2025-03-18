using System.ComponentModel.DataAnnotations;
using MediatR;
using TransactionService.Contract.Enums;
using TransactionService.Contract.Responses;

namespace TransactionService.Contract.Requests;

public class UpdateTransactionStatusRequest : IRequest<UpdateTransactionStatusResponse>
{
    [Required]
    public Guid TransactionId { get; set; }
        
    [Required]
    public TransactionStatusType NewStatus { get; set; }
        
    [StringLength(500)]
    public string Notes { get; set; }
}