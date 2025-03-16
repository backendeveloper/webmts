using AuthService.Contract.Dtos;

namespace AuthService.Business.Services;

public interface ITokenService
{
    Task<(string accessToken, DateTime expiration)> GenerateAccessTokenAsync(UserDto user);
    Task<string> GenerateRefreshTokenAsync(Guid userId);
    Task<bool> ValidateTokenAsync(string token);
    Task<UserDto> GetUserFromTokenAsync(string token);
    Task<bool> IsRefreshTokenValidAsync(Guid userId, string refreshToken);
    Task RevokeRefreshTokenAsync(Guid userId, string refreshToken);
    Task RevokeAllUserTokensAsync(Guid userId);
}