using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AuthService.Common.Caching;
using AuthService.Common.Exceptions;
using AuthService.Contract.Dtos;
using AuthService.Data;
using AuthService.Data.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Business.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TokenService> _logger;

    private const string ACCESS_TOKEN_PREFIX = "access_token:";
    
    public TokenService(
        IConfiguration configuration,
        ICacheService cacheService,
        IUnitOfWork unitOfWork,
        ILogger<TokenService> logger)
    {
        _configuration = configuration;
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

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

        var redisKey = $"{ACCESS_TOKEN_PREFIX}{encodedToken}";
        var userJson = JsonSerializer.Serialize(user);
        await _cacheService.SetAsync(redisKey, userJson, TimeSpan.FromMinutes(expiryMinutes));

        return (encodedToken, expiration);
    }

    public async Task<string> GenerateRefreshTokenAsync(Guid userId)
    {
        try
        {
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            var expiryDays = _configuration.GetValue<int>("TokenSettings:RefreshTokenExpiryDays", 7);
            
            var tokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = refreshToken,
                UserId = userId,
                ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
                IsRevoked = false
            };
            
            await _unitOfWork.RefreshTokens.AddAsync(tokenEntity);
            await _unitOfWork.CompleteAsync();

            return refreshToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating refresh token for user {UserId}", userId);
            throw new BusinessValidationException("Failed to generate refresh token");
        }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;
            
        var redisKey = $"{ACCESS_TOKEN_PREFIX}{token}";
        var userJson = await _cacheService.GetAsync<string>(redisKey);
        
        return !string.IsNullOrEmpty(userJson);
    }

    public async Task<UserDto> GetUserFromTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
            return null;
            
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
        if (string.IsNullOrEmpty(refreshToken))
            return false;
            
        return await _unitOfWork.RefreshTokens.IsTokenValidAsync(refreshToken, userId);
    }

    public async Task RevokeRefreshTokenAsync(Guid userId, string refreshToken)
    {
        var token = await _unitOfWork.RefreshTokens.GetByTokenAsync(refreshToken);
        
        if (token != null && token.UserId == userId)
        {
            token.IsRevoked = true;
            await _unitOfWork.CompleteAsync();
        }
    }

    public async Task RevokeAllUserTokensAsync(Guid userId)
    {
        try
        {
            var userTokens = await _unitOfWork.RefreshTokens.GetByUserIdAsync(userId);
            foreach (var token in userTokens) 
                token.IsRevoked = true;
            
            await _unitOfWork.CompleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all tokens for user {UserId}", userId);
            throw new BusinessValidationException("Failed to revoke tokens");
        }
    }
}