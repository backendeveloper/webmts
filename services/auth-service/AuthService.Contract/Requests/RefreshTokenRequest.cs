using AuthService.Contract.Responses;
using MediatR;

namespace AuthService.Contract.Requests;

public class RefreshTokenRequest : IRequest<RefreshTokenResponse>
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
}