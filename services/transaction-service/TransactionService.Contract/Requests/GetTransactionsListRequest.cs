using MediatR;
using TransactionService.Contract.Enums;
using TransactionService.Contract.Responses;

namespace TransactionService.Contract.Requests;

public class GetTransactionsListRequest : IRequest<GetTransactionsListResponse>
{
    public Guid? CustomerId { get; set; }
    public TransactionType? Type { get; set; }
    public TransactionStatusType? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageSize { get; set; } = 10;
    public int PageNumber { get; set; } = 1;
}