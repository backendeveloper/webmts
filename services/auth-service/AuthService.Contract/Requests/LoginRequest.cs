using AuthService.Contract.Responses;
using MediatR;

namespace AuthService.Contract.Requests;

public class LoginRequest : IRequest<LoginResponse>
{
    public string Username { get; set; }
    public string Password { get; set; }
}