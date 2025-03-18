using AuthService.Contract.Responses;
using MediatR;

namespace AuthService.Contract.Requests;

public class UpdateUserRequest : IRequest<UpdateUserResponse>
{
    public Guid UserId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }
    public List<string> Roles { get; set; }
}