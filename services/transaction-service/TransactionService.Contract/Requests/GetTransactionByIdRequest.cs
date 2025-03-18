using System.ComponentModel.DataAnnotations;
using MediatR;
using TransactionService.Contract.Responses;

namespace TransactionService.Contract.Requests;

public class GetTransactionByIdRequest : IRequest<GetTransactionByIdResponse>
{
    [Required]
    public Guid TransactionId { get; set; }
}