using CustomerService.Contract.Responses;
using MediatR;

namespace CustomerService.Contract.Requests;

public class DeleteCustomerRequest : IRequest<DeleteCustomerResponse>
{
    public Guid CustomerId { get; set; }
}