using AuthService.Business.Services;
using AuthService.Contract.Requests;
using AuthService.Contract.Responses;
using MediatR;

namespace AuthService.Business.Handlers;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenRequest, RefreshTokenResponse>
{
    private readonly IAuthService _authService;

    public RefreshTokenHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<RefreshTokenResponse> Handle(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        return await _authService.RefreshTokenAsync(request);
    }
}