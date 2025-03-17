using System.Text.RegularExpressions;
using AuthService.Common.Exceptions;
using AuthService.Contract.Dtos;
using AuthService.Contract.Requests;
using AuthService.Contract.Responses;
using AuthService.Data;
using AuthService.Data.Entities;
using Microsoft.Extensions.Logging;

namespace AuthService.Business.Services;

public class AuthService : IAuthService
{
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ITokenService tokenService,
        IUnitOfWork unitOfWork,
        ILogger<AuthService> logger)
    {
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByUsernameAsync(request.Username);
            if (user == null)
                return new LoginResponse { Success = false, Message = "Invalid username or password" };

            var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!passwordValid)
                return new LoginResponse { Success = false, Message = "Invalid username or password" };

            user.LastLoginAt = DateTime.UtcNow;
            await _unitOfWork.CompleteAsync();

            var userWithRoles = await _unitOfWork.Users.GetWithRolesAsync(user.Id);
            var userDto = MapToUserDto(userWithRoles);

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
        try
        {
            var validationResult = ValidateRegisterRequest(request);
            if (!validationResult.IsValid)
                return new RegisterResponse { Success = false, Message = validationResult.Message };

            if (await _unitOfWork.Users.AnyAsync(u => u.Username == request.Username))
                return new RegisterResponse { Success = false, Message = "Username is already taken" };

            if (await _unitOfWork.Users.AnyAsync(u => u.Email == request.Email))
                return new RegisterResponse { Success = false, Message = "Email is already registered" };

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var newUser = new User
                {
                    Id = Guid.NewGuid(),
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
                };

                await _unitOfWork.Users.AddAsync(newUser);
                await _unitOfWork.CompleteAsync();

                var userRole = new UserRole
                {
                    UserId = newUser.Id,
                    RoleId = 2
                };

                await _unitOfWork.CommitTransactionAsync();

                var userWithRoles = await _unitOfWork.Users.GetWithRolesAsync(newUser.Id);
                var userDto = MapToUserDto(userWithRoles);

                return new RegisterResponse
                {
                    Success = true,
                    Message = "Registration successful",
                    User = userDto
                };
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for user {Username}", request.Username);
            return new RegisterResponse { Success = false, Message = "An error occurred during registration" };
        }
    }

    public async Task<ValidateTokenResponse> ValidateTokenAsync(ValidateTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return new ValidateTokenResponse { IsValid = false };

        try
        {
            var isValid = await _tokenService.ValidateTokenAsync(request.Token);
            if (!isValid)
                return new ValidateTokenResponse { IsValid = false };

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
            return new RefreshTokenResponse { Success = false, Message = "Invalid tokens" };

        try
        {
            var user = await _tokenService.GetUserFromTokenAsync(request.AccessToken);
            if (user == null)
                return new RefreshTokenResponse { Success = false, Message = "Invalid access token" };

            var isValid = await _tokenService.IsRefreshTokenValidAsync(user.Id, request.RefreshToken);
            if (!isValid)
                return new RefreshTokenResponse { Success = false, Message = "Invalid refresh token" };

            await _tokenService.RevokeRefreshTokenAsync(user.Id, request.RefreshToken);

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
            throw new BusinessValidationException(["Invalid tokens"]);

        try
        {
            var user = await _tokenService.GetUserFromTokenAsync(accessToken);
            if (user == null)
                throw new BusinessValidationException(["Invalid access token"]);

            await _tokenService.RevokeRefreshTokenAsync(user.Id, refreshToken);
        }
        catch (BusinessValidationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            throw new BusinessValidationException("An error occurred during logout");
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
            throw new BusinessValidationException(["An error occurred while revoking tokens"]);
        }
    }

    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? []
        };
    }

    private static (bool IsValid, string Message) ValidateRegisterRequest(RegisterRequest request)
    {
        if (request.Password != request.ConfirmPassword)
            return (false, "Passwords do not match");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            return (false, "Password must be at least 8 characters long");

        return (!IsValidEmail(request.Email) ? (false, "Invalid email format") : (true, null))!;
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
        return emailRegex.IsMatch(email);
    }
}