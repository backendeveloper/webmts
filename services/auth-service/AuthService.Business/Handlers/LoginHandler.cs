using AuthService.Business.Services;
using AuthService.Contract.Requests;
using AuthService.Contract.Responses;
using MediatR;

namespace AuthService.Business.Handlers;

public class LoginHandler : IRequestHandler<LoginRequest, LoginResponse>
{
    private readonly IAuthService _authService;

    public LoginHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<LoginResponse> Handle(LoginRequest request, CancellationToken cancellationToken)
    {
        return await _authService.LoginAsync(request);
    }
}