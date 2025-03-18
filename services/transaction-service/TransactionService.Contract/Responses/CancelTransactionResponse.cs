using TransactionService.Contract.Dtos;

namespace TransactionService.Contract.Responses;

public class CancelTransactionResponse : BaseResponse
{
    public TransactionDto Transaction { get; set; }
}