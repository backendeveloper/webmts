using AuthService.Contract.Responses;
using MediatR;

namespace AuthService.Contract.Requests;

public class DeleteUserRequest : IRequest<DeleteUserResponse>
{
    public Guid UserId { get; set; }
}