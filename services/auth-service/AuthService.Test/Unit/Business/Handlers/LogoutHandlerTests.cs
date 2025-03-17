using AuthService.Business.Handlers;
using AuthService.Business.Services;
using AuthService.Contract.Requests;
using FluentAssertions;
using Moq;
using Xunit;

namespace AuthService.Test.Unit.Business.Handlers;

public class LogoutHandlerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly LogoutHandler _sut;

    public LogoutHandlerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _sut = new LogoutHandler(_mockAuthService.Object);
    }

    [Fact]
    public async Task Handle_ShouldCallAuthServiceLogoutAsync()
    {
        // Arrange
        var request = new LogoutRequest
        {
            AccessToken = "access_token",
            RefreshToken = "refresh_token"
        };

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        // result.Should().Be(Unit.Value);
        _mockAuthService.Verify(x => x.LogoutAsync(request.AccessToken, request.RefreshToken), Times.Once);
    }
}