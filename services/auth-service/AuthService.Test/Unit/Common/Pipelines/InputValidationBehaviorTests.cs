using AuthService.Common.Pipelines;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using Xunit;

namespace AuthService.Test.Unit.Common.Pipelines;

public class InputValidationBehaviorTests
{
    private class TestRequest : IRequest<string>
    {
    }

    [Fact]
    public async Task Handle_ShouldContinue_WhenNoValidators()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>>();
        var behavior = new InputValidationBehavior<TestRequest, string>(validators);
        var nextMock = new Mock<RequestHandlerDelegate<string>>();
        nextMock.Setup(x => x()).ReturnsAsync("Success");

        // Act
        var result = await behavior.Handle(new TestRequest(), nextMock.Object, CancellationToken.None);

        // Assert
        Assert.Equal("Success", result);
        nextMock.Verify(x => x(), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldContinue_WhenValidationSucceeds()
    {
        // Arrange
        var validatorMock = new Mock<IValidator<TestRequest>>();
        validatorMock.Setup(x =>
                x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var validators = new List<IValidator<TestRequest>> { validatorMock.Object };
        var behavior = new InputValidationBehavior<TestRequest, string>(validators);
        var nextMock = new Mock<RequestHandlerDelegate<string>>();
        nextMock.Setup(x => x()).ReturnsAsync("Success");

        // Act
        var result = await behavior.Handle(new TestRequest(), nextMock.Object, CancellationToken.None);

        // Assert
        Assert.Equal("Success", result);
        nextMock.Verify(x => x(), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowException_WhenValidationFails()
    {
        // Arrange
        var failures = new List<ValidationFailure> { new("Property", "Error") };
        var validationResult = new ValidationResult(failures);

        var validatorMock = new Mock<IValidator<TestRequest>>();
        validatorMock.Setup(x =>
                x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var validators = new List<IValidator<TestRequest>> { validatorMock.Object };
        var behavior = new InputValidationBehavior<TestRequest, string>(validators);
        var nextMock = new Mock<RequestHandlerDelegate<string>>();

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(new TestRequest(), nextMock.Object, CancellationToken.None));

        nextMock.Verify(x => x(), Times.Never);
    }
}