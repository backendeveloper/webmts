using TransactionService.Contract.Dtos;

namespace TransactionService.Contract.Responses;

public class CreateTransactionResponse : BaseResponse
{
    public TransactionDto Transaction { get; set; }
}