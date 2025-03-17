using AuthService.Business.Handlers;
using AuthService.Business.Services;
using AuthService.Contract.Requests;
using AuthService.Contract.Responses;
using FluentAssertions;
using Moq;
using Xunit;

namespace AuthService.Test.Unit.Business.Handlers;

public class LoginHandlerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly LoginHandler _sut;

    public LoginHandlerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _sut = new LoginHandler(_mockAuthService.Object);
    }

    [Fact]
    public async Task Handle_ShouldCallAuthServiceLoginAsync()
    {
        // Arrange
        var request = new LoginRequest
        {
            Username = "testuser",
            Password = "password123"
        };

        var expectedResponse = new LoginResponse
        {
            Success = true,
            AccessToken = "access_token",
            RefreshToken = "refresh_token"
        };

        _mockAuthService.Setup(x => x.LoginAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedResponse);
        _mockAuthService.Verify(x => x.LoginAsync(request), Times.Once);
    }
}