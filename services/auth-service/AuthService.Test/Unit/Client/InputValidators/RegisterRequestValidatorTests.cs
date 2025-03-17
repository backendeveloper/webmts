using AuthService.Client.InputValidators;
using AuthService.Contract.Requests;
using FluentValidation.TestHelper;
using Xunit;

namespace AuthService.Test.Unit.Client.InputValidators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator;

    public RegisterRequestValidatorTests()
    {
        _validator = new RegisterRequestValidator();
    }

    [Fact]
    public void ShouldHaveError_WhenUsernameIsEmpty()
    {
        // Arrange
        var model = new RegisterRequest
        {
            Username = "",
            Email = "test@example.com",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void ShouldHaveError_WhenUsernameTooShort()
    {
        // Arrange
        var model = new RegisterRequest
        {
            Username = "ab",
            Email = "test@example.com",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username must be at least 3 characters long.");
    }

    [Fact]
    public void ShouldHaveError_WhenEmailIsEmpty()
    {
        // Arrange
        var model = new RegisterRequest
        {
            Username = "testuser",
            Email = "",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void ShouldHaveError_WhenEmailFormatIsInvalid()
    {
        // Arrange
        var model = new RegisterRequest
        {
            Username = "testuser",
            Email = "invalid-email",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Invalid email format.");
    }

    [Fact]
    public void ShouldHaveError_WhenPasswordIsEmpty()
    {
        // Arrange
        var model = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "",
            ConfirmPassword = ""
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void ShouldHaveError_WhenPasswordTooShort()
    {
        // Arrange
        var model = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "pass",
            ConfirmPassword = "pass"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 8 characters long.");
    }

    [Fact]
    public void ShouldHaveError_WhenPasswordsDoNotMatch()
    {
        // Arrange
        var model = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "password123",
            ConfirmPassword = "different"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword)
            .WithErrorMessage("Passwords do not match.");
    }

    [Fact]
    public void ShouldNotHaveError_WhenModelIsValid()
    {
        // Arrange
        var model = new RegisterRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "password123",
            ConfirmPassword = "password123"
        };

        // Act
        var result = _validator.TestValidate(model);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}