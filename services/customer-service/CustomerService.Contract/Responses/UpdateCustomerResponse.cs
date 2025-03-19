using CustomerService.Contract.Dtos;

namespace CustomerService.Contract.Responses;

public class UpdateCustomerResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public CustomerDto Customer { get; set; }
}