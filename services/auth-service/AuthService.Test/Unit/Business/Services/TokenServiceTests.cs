using System.IdentityModel.Tokens.Jwt;
using AuthService.Business.Services;
using AuthService.Common.Caching;
using AuthService.Contract.Dtos;
using AuthService.Data;
using AuthService.Data.Entities;
using AuthService.Data.Repositories.Imp;
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AuthService.Test.Unit.Business.Services;

public class TokenServiceTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<TokenService>> _mockLogger;
    private readonly Fixture _fixture;
    private readonly TokenService _sut;

    public TokenServiceTests()
    {
        _fixture = new Fixture();

        _mockConfiguration = new Mock<IConfiguration>();
        _mockCacheService = new Mock<ICacheService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<TokenService>>();

        var mockConfigSection = new Mock<IConfigurationSection>();
        mockConfigSection.Setup(x => x.Value).Returns("YourSuperSecretKey-MoreThan256BitsLongForHS256");
        _mockConfiguration.Setup(x => x.GetSection("Jwt:Key")).Returns(mockConfigSection.Object);

        _mockConfiguration.Setup(x => x.GetSection("Jwt:Issuer").Value).Returns("WebmtsAuthService");
        _mockConfiguration.Setup(x => x.GetSection("Jwt:Audience").Value).Returns("WebmtsClient");
        _mockConfiguration.Setup(x => x.GetValue<int>("TokenSettings:AccessTokenExpiryMinutes", 60)).Returns(60);
        _mockConfiguration.Setup(x => x.GetValue<int>("TokenSettings:RefreshTokenExpiryDays", 7)).Returns(7);

        _sut = new TokenService(
            _mockConfiguration.Object,
            _mockCacheService.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GenerateAccessTokenAsync_ShouldCreateToken_WithCorrectClaims()
    {
        var userDto = _fixture.Create<UserDto>();

        var (token, expiration) = await _sut.GenerateAccessTokenAsync(userDto);

        token.Should().NotBeNullOrEmpty();

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(token);

        jwtToken.Subject.Should().Be(userDto.Id.ToString());
        jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value.Should().Be(userDto.Email);
        jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Name).Value.Should().Be(userDto.Username);

        userDto.Roles.ForEach(role =>
            jwtToken.Claims.Any(c => c.Type == "role" && c.Value == role).Should().BeTrue());

        var redisKey = $"access_token:{token}";
        _mockCacheService.Verify(x => x.SetAsync(
                It.Is<string>(s => s == redisKey),
                It.IsAny<string>(),
                It.IsAny<TimeSpan>()),
            Times.Once);
    }
    
    [Fact]
        public async Task GenerateRefreshTokenAsync_ShouldCreateAndSaveToken()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
            _mockUnitOfWork.Setup(x => x.RefreshTokens).Returns(mockRefreshTokenRepo.Object);
            
            // Act
            var refreshToken = await _sut.GenerateRefreshTokenAsync(userId);
            
            // Assert
            refreshToken.Should().NotBeNullOrEmpty();
            
            // Verify saved to repository
            mockRefreshTokenRepo.Verify(x => x.AddAsync(It.Is<RefreshToken>(t => 
                t.UserId == userId && 
                t.Token == refreshToken && 
                !t.IsRevoked)), 
                Times.Once);
            
            // Verify complete was called
            _mockUnitOfWork.Verify(x => x.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task ValidateTokenAsync_ShouldReturnFalse_WhenTokenNotInCache()
        {
            // Arrange
            var token = "some_token";
            _mockCacheService.Setup(x => x.GetAsync<string>($"access_token:{token}"))
                .ReturnsAsync((string)null!);
            
            // Act
            var result = await _sut.ValidateTokenAsync(token);
            
            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateTokenAsync_ShouldReturnTrue_WhenTokenInCache()
        {
            // Arrange
            var token = "some_token";
            _mockCacheService.Setup(x => x.GetAsync<string>($"access_token:{token}"))
                .ReturnsAsync("some_user_data");
            
            // Act
            var result = await _sut.ValidateTokenAsync(token);
            
            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task GetUserFromTokenAsync_ShouldReturnUser_WhenTokenValid()
        {
            // Arrange
            var token = "some_token";
            var userDto = _fixture.Create<UserDto>();
            var userJson = System.Text.Json.JsonSerializer.Serialize(userDto);
            
            _mockCacheService.Setup(x => x.GetAsync<string>($"access_token:{token}"))
                .ReturnsAsync(userJson);
            
            // Act
            var result = await _sut.GetUserFromTokenAsync(token);
            
            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(userDto.Id);
            result.Username.Should().Be(userDto.Username);
            result.Email.Should().Be(userDto.Email);
            result.Roles.Should().BeEquivalentTo(userDto.Roles);
        }

        [Fact]
        public async Task IsRefreshTokenValidAsync_ShouldCallRepository()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var token = "refresh_token";
            var mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
            
            mockRefreshTokenRepo.Setup(x => x.IsTokenValidAsync(token, userId))
                .ReturnsAsync(true);
                
            _mockUnitOfWork.Setup(x => x.RefreshTokens)
                .Returns(mockRefreshTokenRepo.Object);
            
            // Act
            var result = await _sut.IsRefreshTokenValidAsync(userId, token);
            
            // Assert
            result.Should().BeTrue();
            mockRefreshTokenRepo.Verify(x => x.IsTokenValidAsync(token, userId), Times.Once);
        }

        [Fact]
        public async Task RevokeRefreshTokenAsync_ShouldUpdateTokenStatus()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var token = "refresh_token";
            var refreshToken = new RefreshToken 
            { 
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = token,
                IsRevoked = false,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };
            
            var mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
            mockRefreshTokenRepo.Setup(x => x.GetByTokenAsync(token))
                .ReturnsAsync(refreshToken);
                
            _mockUnitOfWork.Setup(x => x.RefreshTokens)
                .Returns(mockRefreshTokenRepo.Object);
            
            // Act
            await _sut.RevokeRefreshTokenAsync(userId, token);
            
            // Assert
            refreshToken.IsRevoked.Should().BeTrue();
            _mockUnitOfWork.Verify(x => x.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task RevokeAllUserTokensAsync_ShouldRevokeAllTokens()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var tokens = new List<RefreshToken>
            {
                new() { Id = Guid.NewGuid(), UserId = userId, Token = "token1", IsRevoked = false },
                new() { Id = Guid.NewGuid(), UserId = userId, Token = "token2", IsRevoked = false }
            };
            
            var mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
            mockRefreshTokenRepo.Setup(x => x.GetByUserIdAsync(userId))
                .ReturnsAsync(tokens);
                
            _mockUnitOfWork.Setup(x => x.RefreshTokens)
                .Returns(mockRefreshTokenRepo.Object);
            
            // Act
            await _sut.RevokeAllUserTokensAsync(userId);
            
            // Assert
            tokens.All(t => t.IsRevoked).Should().BeTrue();
            _mockUnitOfWork.Verify(x => x.CompleteAsync(), Times.Once);
        }
}