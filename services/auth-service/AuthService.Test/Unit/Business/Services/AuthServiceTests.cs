using AuthService.Business.Services;
using AuthService.Common.Exceptions;
using AuthService.Contract.Dtos;
using AuthService.Contract.Requests;
using AuthService.Data;
using AuthService.Data.Entities;
using AuthService.Data.Repositories.Imp;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AuthService.Test.Unit.Business.Services;

public class AuthServiceTests
{
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<AuthService.Business.Services.AuthService>> _mockLogger;
    private readonly Fixture _fixture;
    private readonly AuthService.Business.Services.AuthService _sut;

    public AuthServiceTests()
    {
        _fixture = new Fixture();
        _mockTokenService = new Mock<ITokenService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<AuthService.Business.Services.AuthService>>();

        _sut = new AuthService.Business.Services.AuthService(
            _mockTokenService.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnSuccessResponse()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "password123"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            UserRoles = new List<UserRole>
            {
                new() { Role = new Role { Id = 2, Name = "User" } }
            }
        };

        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(x => x.GetByUsernameAsync(request.Username))
            .ReturnsAsync(user);
        mockUserRepo.Setup(x => x.GetWithRolesAsync(user.Id))
            .ReturnsAsync(user);

        _mockUnitOfWork.Setup(x => x.Users).Returns(mockUserRepo.Object);

        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Roles = new List<string> { "User" }
        };

        _mockTokenService.Setup(x => x.GenerateAccessTokenAsync(It.IsAny<UserDto>()))
            .ReturnsAsync(("access_token", DateTime.UtcNow.AddHours(1)));
        _mockTokenService.Setup(x => x.GenerateRefreshTokenAsync(user.Id))
            .ReturnsAsync("refresh_token");

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");
        result.User.Should().NotBeNull();
        result.User.Id.Should().Be(user.Id);
        result.User.Username.Should().Be(user.Username);
        result.User.Email.Should().Be(user.Email);
        result.User.Roles.Should().ContainSingle(r => r == "User");

        // Verify user.LastLoginAt was updated
        user.LastLoginAt.Should().NotBeNull();
        _mockUnitOfWork.Verify(x => x.CompleteAsync(), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidUsername_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "nonexistent",
            Password = "password123"
        };

        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(x => x.GetByUsernameAsync(request.Username))
            .ReturnsAsync((User)null);

        _mockUnitOfWork.Setup(x => x.Users).Returns(mockUserRepo.Object);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid username or password");
        result.AccessToken.Should().BeNull();
        result.RefreshToken.Should().BeNull();
        result.User.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "wrongpassword"
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = "test@example.com",
            // Hash for a different password
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("correctpassword")
        };

        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(x => x.GetByUsernameAsync(request.Username))
            .ReturnsAsync(user);

        _mockUnitOfWork.Setup(x => x.Users).Returns(mockUserRepo.Object);

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid username or password");
        result.AccessToken.Should().BeNull();
        result.RefreshToken.Should().BeNull();
        result.User.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldReturnSuccessResponse()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(false);
        mockUserRepo.Setup(x => x.GetWithRolesAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new User
            {
                Id = Guid.NewGuid(),
                Username = request.Username,
                Email = request.Email,
                UserRoles = new List<UserRole>
                {
                    new() { Role = new Role { Id = 2, Name = "User" } }
                }
            });

        _mockUnitOfWork.Setup(x => x.Users).Returns(mockUserRepo.Object);
        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Registration successful");
        result.User.Should().NotBeNull();
        result.User.Username.Should().Be(request.Username);
        result.User.Email.Should().Be(request.Email);
        result.User.Roles.Should().ContainSingle(r => r == "User");

        mockUserRepo.Verify(x => x.AddAsync(It.Is<User>(u =>
                u.Username == request.Username &&
                u.Email == request.Email)),
            Times.Once);
        _mockUnitOfWork.Verify(x => x.CompleteAsync(), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingUsername_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "existinguser",
            Email = "newuser@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var mockUserRepo = new Mock<IUserRepository>();
        mockUserRepo.Setup(x => x.AnyAsync(It.Is<System.Linq.Expressions.Expression<Func<User, bool>>>(
                expr => expr.Compile()(new User { Username = "existinguser" }))))
            .ReturnsAsync(true);

        _mockUnitOfWork.Setup(x => x.Users).Returns(mockUserRepo.Object);

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Username is already taken");
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "existing@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };

        var mockUserRepo = new Mock<IUserRepository>();
        // First call is for username check which passes
        mockUserRepo.SetupSequence(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>()))
            .ReturnsAsync(false) // Username doesn't exist
            .ReturnsAsync(true); // Email exists

        _mockUnitOfWork.Setup(x => x.Users).Returns(mockUserRepo.Object);

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Email is already registered");
    }

    [Fact]
    public async Task RegisterAsync_WithPasswordMismatch_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "Password123!",
            ConfirmPassword = "DifferentPassword!"
        };

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Passwords do not match");
    }

    [Fact]
    public async Task RegisterAsync_WithWeakPassword_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "newuser@example.com",
            Password = "short",
            ConfirmPassword = "short"
        };

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Password must be at least 8 characters long");
    }

    [Fact]
    public async Task ValidateTokenAsync_WithValidToken_ShouldReturnValidResponse()
    {
        // Arrange
        var request = new ValidateTokenRequest { Token = "valid_token" };
        var userDto = _fixture.Create<UserDto>();

        _mockTokenService.Setup(x => x.ValidateTokenAsync(request.Token))
            .ReturnsAsync(true);
        _mockTokenService.Setup(x => x.GetUserFromTokenAsync(request.Token))
            .ReturnsAsync(userDto);

        // Act
        var result = await _sut.ValidateTokenAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.User.Should().BeEquivalentTo(userDto);
    }

    [Fact]
    public async Task ValidateTokenAsync_WithInvalidToken_ShouldReturnInvalidResponse()
    {
        // Arrange
        var request = new ValidateTokenRequest { Token = "invalid_token" };

        _mockTokenService.Setup(x => x.ValidateTokenAsync(request.Token))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.ValidateTokenAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.User.Should().BeNull();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidTokens_ShouldReturnNewTokens()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new RefreshTokenRequest
        {
            AccessToken = "old_access_token",
            RefreshToken = "old_refresh_token"
        };

        var userDto = new UserDto
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            Roles = new List<string> { "User" }
        };

        _mockTokenService.Setup(x => x.GetUserFromTokenAsync(request.AccessToken))
            .ReturnsAsync(userDto);
        _mockTokenService.Setup(x => x.IsRefreshTokenValidAsync(userId, request.RefreshToken))
            .ReturnsAsync(true);
        _mockTokenService.Setup(x => x.GenerateAccessTokenAsync(userDto))
            .ReturnsAsync(("new_access_token", DateTime.UtcNow.AddHours(1)));
        _mockTokenService.Setup(x => x.GenerateRefreshTokenAsync(userId))
            .ReturnsAsync("new_refresh_token");

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("new_access_token");
        result.RefreshToken.Should().Be("new_refresh_token");

        _mockTokenService.Verify(x => x.RevokeRefreshTokenAsync(userId, request.RefreshToken), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidAccessToken_ShouldReturnFailureResponse()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            AccessToken = "invalid_access_token",
            RefreshToken = "refresh_token"
        };

        _mockTokenService.Setup(x => x.GetUserFromTokenAsync(request.AccessToken))
            .ReturnsAsync((UserDto)null);

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid access token");
    }

    [Fact]
    public async Task RefreshTokenAsync_WithInvalidRefreshToken_ShouldReturnFailureResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new RefreshTokenRequest
        {
            AccessToken = "valid_access_token",
            RefreshToken = "invalid_refresh_token"
        };

        var userDto = new UserDto
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            Roles = new List<string> { "User" }
        };

        _mockTokenService.Setup(x => x.GetUserFromTokenAsync(request.AccessToken))
            .ReturnsAsync(userDto);
        _mockTokenService.Setup(x => x.IsRefreshTokenValidAsync(userId, request.RefreshToken))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.RefreshTokenAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid refresh token");
    }

    [Fact]
    public async Task LogoutAsync_WithValidTokens_ShouldRevokeRefreshToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var accessToken = "valid_access_token";
        var refreshToken = "valid_refresh_token";

        var userDto = new UserDto
        {
            Id = userId,
            Username = "testuser",
            Email = "test@example.com",
            Roles = new List<string> { "User" }
        };

        _mockTokenService.Setup(x => x.GetUserFromTokenAsync(accessToken))
            .ReturnsAsync(userDto);

        // Act & Assert
        await _sut.LogoutAsync(accessToken, refreshToken);

        // Verify refresh token was revoked
        _mockTokenService.Verify(x => x.RevokeRefreshTokenAsync(userId, refreshToken), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_WithInvalidAccessToken_ShouldThrowException()
    {
        // Arrange
        var accessToken = "invalid_access_token";
        var refreshToken = "refresh_token";

        _mockTokenService.Setup(x => x.GetUserFromTokenAsync(accessToken))
            .ReturnsAsync((UserDto)null);

        // Act & Assert
        await Assert.ThrowsAsync<BusinessValidationException>(() =>
            _sut.LogoutAsync(accessToken, refreshToken));
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_ShouldCallTokenService()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _sut.RevokeAllUserTokensAsync(userId);

        // Assert
        _mockTokenService.Verify(x => x.RevokeAllUserTokensAsync(userId), Times.Once);
    }
}