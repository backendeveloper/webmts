namespace TransactionService.Contract.Dtos;

public class TransactionHistoryDto
{
    public Guid Id { get; set; }
    public string OldStatus { get; set; }
    public string NewStatus { get; set; }
    public string Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
}