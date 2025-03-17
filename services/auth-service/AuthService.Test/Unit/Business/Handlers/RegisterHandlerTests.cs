using AuthService.Business.Handlers;
using AuthService.Business.Services;
using AuthService.Contract.Requests;
using AuthService.Contract.Responses;
using FluentAssertions;
using Moq;
using Xunit;

namespace AuthService.Test.Unit.Business.Handlers;

public class RegisterHandlerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly RegisterHandler _sut;

    public RegisterHandlerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _sut = new RegisterHandler(_mockAuthService.Object);
    }

    [Fact]
    public async Task Handle_ShouldCallAuthServiceRegisterAsync()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "new@example.com",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        var expectedResponse = new RegisterResponse
        {
            Success = true,
            Message = "Registration successful"
        };

        _mockAuthService.Setup(x => x.RegisterAsync(request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedResponse);
        _mockAuthService.Verify(x => x.RegisterAsync(request), Times.Once);
    }
}