using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AuthService.Common.Caching;
using AuthService.Contract.Dtos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Business.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ICacheService _cacheService;
    private readonly ILogger<TokenService> _logger;

    // Redis key prefixes
    private const string ACCESS_TOKEN_PREFIX = "access_token:";
    private const string REFRESH_TOKEN_PREFIX = "refresh_token:";
    private const string USER_REFRESH_TOKENS_PREFIX = "user_refresh_tokens:";
    
    public async Task<(string accessToken, DateTime expiration)> GenerateAccessTokenAsync(UserDto user)
    {
        var expiryMinutes = _configuration.GetValue("TokenSettings:AccessTokenExpiryMinutes", 60);
        var expiration = DateTime.UtcNow.AddMinutes(expiryMinutes);
        
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"));
        
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        // Add roles as claims
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiration,
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var encodedToken = tokenHandler.WriteToken(token);

        // Store token info in Redis
        var redisKey = $"{ACCESS_TOKEN_PREFIX}{encodedToken}";
        var userJson = JsonSerializer.Serialize(user);
        await _cacheService.SetAsync(redisKey, userJson, TimeSpan.FromMinutes(expiryMinutes));

        return (encodedToken, expiration);
    }

    public async Task<string> GenerateRefreshTokenAsync(Guid userId)
    {
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var expiryDays = _configuration.GetValue<int>("TokenSettings:RefreshTokenExpiryDays", 7);

        // Store refresh token in Redis
        var redisRefreshKey = $"{REFRESH_TOKEN_PREFIX}{refreshToken}";
        await _cacheService.SetAsync(redisRefreshKey, userId.ToString(), TimeSpan.FromDays(expiryDays));

        // Add to the user's set of refresh tokens
        var userRefreshTokensKey = $"{USER_REFRESH_TOKENS_PREFIX}{userId}";
        
        // Get existing tokens or initialize a new set
        var existingTokens = await _cacheService.GetAsync<List<string>>(userRefreshTokensKey) ?? new List<string>();
        existingTokens.Add(refreshToken);
        
        // Update the set in Redis
        await _cacheService.SetAsync(userRefreshTokensKey, existingTokens, TimeSpan.FromDays(expiryDays * 2));

        return refreshToken;
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        var redisKey = $"{ACCESS_TOKEN_PREFIX}{token}";
        var userJson = await _cacheService.GetAsync<string>(redisKey);
        return !string.IsNullOrEmpty(userJson);
    }

    public async Task<UserDto> GetUserFromTokenAsync(string token)
    {
        var redisKey = $"{ACCESS_TOKEN_PREFIX}{token}";
        var userJson = await _cacheService.GetAsync<string>(redisKey);

        if (string.IsNullOrEmpty(userJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<UserDto>(userJson);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize user from token");
            return null;
        }
    }

    public async Task<bool> IsRefreshTokenValidAsync(Guid userId, string refreshToken)
    {
        var redisRefreshKey = $"{REFRESH_TOKEN_PREFIX}{refreshToken}";
        var storedUserId = await _cacheService.GetAsync<string>(redisRefreshKey);

        if (string.IsNullOrEmpty(storedUserId))
        {
            return false;
        }

        // Also verify this refresh token belongs to the specified user
        var userRefreshTokensKey = $"{USER_REFRESH_TOKENS_PREFIX}{userId}";
        var userTokens = await _cacheService.GetAsync<List<string>>(userRefreshTokensKey) ?? new List<string>();
        
        return storedUserId == userId.ToString() && userTokens.Contains(refreshToken);
    }

    public async Task RevokeRefreshTokenAsync(Guid userId, string refreshToken)
    {
        // Remove the refresh token
        var redisRefreshKey = $"{REFRESH_TOKEN_PREFIX}{refreshToken}";
        await _cacheService.RemoveAsync(redisRefreshKey);

        // Remove from user's set of refresh tokens
        var userRefreshTokensKey = $"{USER_REFRESH_TOKENS_PREFIX}{userId}";
        var userTokens = await _cacheService.GetAsync<List<string>>(userRefreshTokensKey) ?? new List<string>();
        userTokens.Remove(refreshToken);
        
        if (userTokens.Any())
        {
            // Update the set in Redis if it's not empty
            var expiryDays = _configuration.GetValue<int>("TokenSettings:RefreshTokenExpiryDays", 7);
            await _cacheService.SetAsync(userRefreshTokensKey, userTokens, TimeSpan.FromDays(expiryDays * 2));
        }
        else
        {
            // If empty, just remove the entire key
            await _cacheService.RemoveAsync(userRefreshTokensKey);
        }
    }

    public async Task RevokeAllUserTokensAsync(Guid userId)
    {
        // Get all refresh tokens for user
        var userRefreshTokensKey = $"{USER_REFRESH_TOKENS_PREFIX}{userId}";
        var refreshTokens = await _cacheService.GetAsync<List<string>>(userRefreshTokensKey) ?? new List<string>();

        // Delete each refresh token
        foreach (var token in refreshTokens)
        {
            var redisRefreshKey = $"{REFRESH_TOKEN_PREFIX}{token}";
            await _cacheService.RemoveAsync(redisRefreshKey);
        }

        // Delete the set of refresh tokens
        await _cacheService.RemoveAsync(userRefreshTokensKey);
    }
}