using CustomerService.Contract.Responses;
using MediatR;

namespace CustomerService.Contract.Requests;

public class GetCustomerByIdRequest : IRequest<GetCustomerByIdResponse>
{
    public Guid CustomerId { get; set; }
}