using AuthService.Business.Handlers;
using AuthService.Business.Services;
using AuthService.Contract.Requests;
using AuthService.Contract.Responses;
using FluentAssertions;
using Moq;
using Xunit;

namespace AuthService.Test.Unit.Business.Handlers;

public class RefreshTokenHandlerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly RefreshTokenHandler _sut;

    public RefreshTokenHandlerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _sut = new RefreshTokenHandler(_mockAuthService.Object);
    }

    [Fact]
    public async Task Handle_ShouldCallAuthServiceRefreshTokenAsync()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            AccessToken = "access_token",
            RefreshToken = "refresh_token"
        };

        var expectedResponse = new RefreshTokenResponse
        {
            Success = true,
            AccessToken = "new_access_token",
            RefreshToken = "new_refresh_token"
        };

        _mockAuthService.Setup(x => x.RefreshTokenAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedResponse);
        _mockAuthService.Verify(x => x.RefreshTokenAsync(request), Times.Once);
    }
}
