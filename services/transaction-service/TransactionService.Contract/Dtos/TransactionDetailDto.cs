namespace TransactionService.Contract.Dtos;

public class TransactionDetailDto : TransactionDto
{
    public List<TransactionHistoryDto> TransactionHistory { get; set; } = new List<TransactionHistoryDto>();
}