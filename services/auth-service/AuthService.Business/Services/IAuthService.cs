using AuthService.Contract.Requests;
using AuthService.Contract.Responses;

namespace AuthService.Business.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<ValidateTokenResponse> ValidateTokenAsync(ValidateTokenRequest request);
    Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task LogoutAsync(string accessToken, string refreshToken);
    Task RevokeAllUserTokensAsync(Guid userId);
}