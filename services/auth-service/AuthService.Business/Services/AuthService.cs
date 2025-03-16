using System.Text.RegularExpressions;
using AuthService.Business.Models;
using AuthService.Contract.Dtos;
using AuthService.Contract.Requests;
using AuthService.Contract.Responses;
using Microsoft.Extensions.Logging;

namespace AuthService.Business.Services;

public class AuthService : IAuthService
{
    private readonly ITokenService _tokenService;
    private readonly InMemoryUserStore _userStore;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ITokenService tokenService,
        InMemoryUserStore userStore,
        ILogger<AuthService> logger)
    {
        _tokenService = tokenService;
        _userStore = userStore;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = _userStore.FindByUsername(request.Username);
        
        if (user == null)
        {
            return new LoginResponse { Success = false, Message = "Invalid username or password" };
        }

        // Check password using BCrypt
        bool passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        
        if (!passwordValid)
        {
            return new LoginResponse { Success = false, Message = "Invalid username or password" };
        }

        // Update last login time
        _userStore.UpdateLastLogin(user.Id, DateTime.UtcNow);

        var userDto = MapToUserDto(user);
        
        try
        {
            var (accessToken, expiration) = await _tokenService.GenerateAccessTokenAsync(userDto);
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id);

            return new LoginResponse
            {
                Success = true,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpirationTime = expiration,
                User = userDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", request.Username);
            return new LoginResponse { Success = false, Message = "An error occurred during login" };
        }
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        // Validate password
        if (request.Password != request.ConfirmPassword)
        {
            return new RegisterResponse { Success = false, Message = "Passwords do not match" };
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return new RegisterResponse { Success = false, Message = "Password must be at least 8 characters long" };
        }

        // Validate email
        if (!IsValidEmail(request.Email))
        {
            return new RegisterResponse { Success = false, Message = "Invalid email format" };
        }

        // Check if username already exists
        if (_userStore.FindByUsername(request.Username) != null)
        {
            return new RegisterResponse { Success = false, Message = "Username is already taken" };
        }

        // Check if email already exists
        if (_userStore.FindByEmail(request.Email) != null)
        {
            return new RegisterResponse { Success = false, Message = "Email is already registered" };
        }

        // Create new user
        var newUser = new UserModel
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Roles = new List<string> { "User" },
            CreatedAt = DateTime.UtcNow
        };

        _userStore.Add(newUser);

        var userDto = MapToUserDto(newUser);

        return new RegisterResponse
        {
            Success = true,
            Message = "Registration successful",
            User = userDto
        };
    }

    public async Task<ValidateTokenResponse> ValidateTokenAsync(ValidateTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return new ValidateTokenResponse { IsValid = false };
        }

        try
        {
            var isValid = await _tokenService.ValidateTokenAsync(request.Token);
            
            if (!isValid)
            {
                return new ValidateTokenResponse { IsValid = false };
            }

            var user = await _tokenService.GetUserFromTokenAsync(request.Token);
            
            return new ValidateTokenResponse
            {
                IsValid = true,
                User = user
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return new ValidateTokenResponse { IsValid = false };
        }
    }

    public async Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken) || string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return new RefreshTokenResponse { Success = false, Message = "Invalid tokens" };
        }

        try
        {
            // Get user info from the current token
            var user = await _tokenService.GetUserFromTokenAsync(request.AccessToken);
            
            if (user == null)
            {
                return new RefreshTokenResponse { Success = false, Message = "Invalid access token" };
            }

            // Validate the refresh token
            var isValid = await _tokenService.IsRefreshTokenValidAsync(user.Id, request.RefreshToken);
            
            if (!isValid)
            {
                return new RefreshTokenResponse { Success = false, Message = "Invalid refresh token" };
            }

            // Revoke the old refresh token
            await _tokenService.RevokeRefreshTokenAsync(user.Id, request.RefreshToken);

            // Generate new tokens
            var (newAccessToken, expiration) = await _tokenService.GenerateAccessTokenAsync(user);
            var newRefreshToken = await _tokenService.GenerateRefreshTokenAsync(user.Id);

            return new RefreshTokenResponse
            {
                Success = true,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpirationTime = expiration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return new RefreshTokenResponse { Success = false, Message = "An error occurred during token refresh" };
        }
    }

    public async Task LogoutAsync(string accessToken, string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new BusinessValidationException(new[] { "Invalid tokens" });
        }

        try
        {
            // Get user info from access token
            var user = await _tokenService.GetUserFromTokenAsync(accessToken);
            
            if (user == null)
            {
                throw new BusinessValidationException(new[] { "Invalid access token" });
            }

            // Revoke refresh token
            await _tokenService.RevokeRefreshTokenAsync(user.Id, refreshToken);
        }
        catch (BusinessValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            throw new BusinessValidationException(new[] { "An error occurred during logout" });
        }
    }

    public async Task RevokeAllUserTokensAsync(Guid userId)
    {
        try
        {
            await _tokenService.RevokeAllUserTokensAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all tokens for user {UserId}", userId);
            throw new BusinessValidationException(new[] { "An error occurred while revoking tokens" });
        }
    }

    private UserDto MapToUserDto(UserModel user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Roles = user.Roles
        };
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        // Simple regex for basic email validation
        var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
        return emailRegex.IsMatch(email);
    }
}