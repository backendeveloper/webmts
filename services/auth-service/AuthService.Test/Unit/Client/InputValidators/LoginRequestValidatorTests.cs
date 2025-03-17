using AuthService.Client.InputValidators;
using AuthService.Contract.Requests;
using FluentValidation.TestHelper;
using Xunit;

namespace AuthService.Test.Unit.Client.InputValidators;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator;

    public LoginRequestValidatorTests()
    {
        _validator = new LoginRequestValidator();
    }

    [Fact]
    public void ShouldHaveError_WhenUsernameIsEmpty()
    {
        // Arrange
        var model = new LoginRequest { Username = "", Password = "password123" };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void ShouldHaveError_WhenPasswordIsEmpty()
    {
        // Arrange
        var model = new LoginRequest { Username = "testuser", Password = "" };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void ShouldHaveError_WhenPasswordTooShort()
    {
        // Arrange
        var model = new LoginRequest { Username = "testuser", Password = "pass" };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 6 characters long.");
    }

    [Fact]
    public void ShouldNotHaveError_WhenModelIsValid()
    {
        // Arrange
        var model = new LoginRequest { Username = "testuser", Password = "password123" };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}