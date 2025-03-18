using CustomerService.Contract.Responses;
using MediatR;

namespace CustomerService.Contract.Requests;

public class GetCustomersRequest : IRequest<GetCustomersResponse>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}