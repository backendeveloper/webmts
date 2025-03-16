using AuthService.Contract.Responses;
using MediatR;

namespace AuthService.Contract.Requests;

public class ValidateTokenRequest : IRequest<ValidateTokenResponse>
{
    public string Token { get; set; }
}