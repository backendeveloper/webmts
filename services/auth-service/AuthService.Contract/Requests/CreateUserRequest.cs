using AuthService.Contract.Responses;
using MediatR;

namespace AuthService.Contract.Requests;

public class CreateUserRequest : IRequest<CreateUserResponse>
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }
    public List<string> Roles { get; set; } = new List<string>() { "User" };
}