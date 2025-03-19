namespace CustomerService.Contract.Responses;

public class CreateCustomerResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public Guid? CustomerId { get; set; }
}