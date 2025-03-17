using AuthService.Business.Handlers;
using AuthService.Business.Services;
using AuthService.Contract.Requests;
using AuthService.Contract.Responses;
using FluentAssertions;
using Moq;
using Xunit;

namespace AuthService.Test.Unit.Business.Handlers;

public class ValidateTokenHandlerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly ValidateTokenHandler _sut;

    public ValidateTokenHandlerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _sut = new ValidateTokenHandler(_mockAuthService.Object);
    }

    [Fact]
    public async Task Handle_ShouldCallAuthServiceValidateTokenAsync()
    {
        // Arrange
        var request = new ValidateTokenRequest
        {
            Token = "some_token"
        };

        var expectedResponse = new ValidateTokenResponse
        {
            IsValid = true
        };

        _mockAuthService.Setup(x => x.ValidateTokenAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedResponse);
        _mockAuthService.Verify(x => x.ValidateTokenAsync(request), Times.Once);
    }
}