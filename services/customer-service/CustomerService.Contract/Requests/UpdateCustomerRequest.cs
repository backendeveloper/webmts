using CustomerService.Contract.Responses;
using MediatR;

namespace CustomerService.Contract.Requests;

public class UpdateCustomerRequest : IRequest<UpdateCustomerResponse>
{
    public Guid CustomerId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
}