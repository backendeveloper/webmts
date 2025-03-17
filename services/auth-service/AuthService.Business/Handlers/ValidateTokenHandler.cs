using AuthService.Business.Services;
using AuthService.Contract.Requests;
using AuthService.Contract.Responses;
using MediatR;

namespace AuthService.Business.Handlers;

public class ValidateTokenHandler : IRequestHandler<ValidateTokenRequest, ValidateTokenResponse>
{
    private readonly IAuthService _authService;

    public ValidateTokenHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<ValidateTokenResponse> Handle(ValidateTokenRequest request, CancellationToken cancellationToken)
    {
        return await _authService.ValidateTokenAsync(request);
    }
}