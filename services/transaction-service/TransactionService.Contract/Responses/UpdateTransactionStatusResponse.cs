using TransactionService.Contract.Dtos;

namespace TransactionService.Contract.Responses;

public class UpdateTransactionStatusResponse : BaseResponse
{
    public TransactionDto Transaction { get; set; }
}