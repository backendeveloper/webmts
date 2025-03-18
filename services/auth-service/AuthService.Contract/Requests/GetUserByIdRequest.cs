using AuthService.Contract.Responses;
using MediatR;

namespace AuthService.Contract.Requests;

public class GetUserByIdRequest : IRequest<GetUserByIdResponse>
{
    public Guid UserId { get; set; }
}