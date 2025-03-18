using CustomerService.Contract.Responses;
using MediatR;

namespace CustomerService.Contract.Requests;

public class CreateCustomerRequest : IRequest<CreateCustomerResponse>
{
    public string Username { get; set; }
    public string Email { get; set; }
}