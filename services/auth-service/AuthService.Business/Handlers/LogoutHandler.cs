using AuthService.Business.Services;
using AuthService.Contract.Requests;
using MediatR;

namespace AuthService.Business.Handlers;

public class LogoutHandler : IRequestHandler<LogoutRequest, Unit>
{
    private readonly IAuthService _authService;

    public LogoutHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<Unit> Handle(LogoutRequest request, CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(request.AccessToken, request.RefreshToken);
        return Unit.Value;
    }
}