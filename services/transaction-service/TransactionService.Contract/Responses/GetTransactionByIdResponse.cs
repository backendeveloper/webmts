using TransactionService.Contract.Dtos;

namespace TransactionService.Contract.Responses;

public class GetTransactionByIdResponse : BaseResponse
{
    public TransactionDetailDto Transaction { get; set; }
}