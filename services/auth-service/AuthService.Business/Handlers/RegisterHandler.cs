using AuthService.Business.Services;
using AuthService.Contract.Requests;
using AuthService.Contract.Responses;
using MediatR;

namespace AuthService.Business.Handlers;

public class RegisterHandler : IRequestHandler<RegisterRequest, RegisterResponse>
{
    private readonly IAuthService _authService;

    public RegisterHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<RegisterResponse> Handle(RegisterRequest request, CancellationToken cancellationToken)
    {
        return await _authService.RegisterAsync(request);
    }
}