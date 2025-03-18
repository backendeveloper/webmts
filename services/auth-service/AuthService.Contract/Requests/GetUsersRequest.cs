using AuthService.Contract.Responses;
using MediatR;

namespace AuthService.Contract.Requests;

public class GetUsersRequest : IRequest<GetUsersResponse>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}