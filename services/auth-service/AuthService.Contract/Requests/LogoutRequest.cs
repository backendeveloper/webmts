using MediatR;

namespace AuthService.Contract.Requests;

public class LogoutRequest : IRequest<Unit>
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}