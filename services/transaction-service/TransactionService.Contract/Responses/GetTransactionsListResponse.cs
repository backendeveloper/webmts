using TransactionService.Contract.Dtos;

namespace TransactionService.Contract.Responses;

public class GetTransactionsListResponse : BaseResponse
{
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public List<TransactionDto> Transactions { get; set; } = new List<TransactionDto>();
}