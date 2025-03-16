using AuthService.Contract.Responses;
using MediatR;

namespace AuthService.Contract.Requests;

public class RegisterRequest : IRequest<RegisterResponse>
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string ConfirmPassword { get; set; }
}