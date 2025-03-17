using AuthService.Common;
using AuthService.Common.Exceptions;
using AuthService.Common.Pipelines;
using MediatR;
using Moq;
using Xunit;

namespace AuthService.Test.Unit.Common.Pipelines;

public class BusinessValidationBehaviorTests
{
    private class TestRequest : IRequest<string>
    {
    }

    [Fact]
    public async Task Handle_ShouldContinue_WhenNoValidators()
    {
        // Arrange
        var businessRules = new List<IBusinessRule<TestRequest>>();
        var behavior = new BusinessValidationBehavior<TestRequest, string>(businessRules);
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
        var ruleMock = new Mock<IBusinessRule<TestRequest>>();
        ruleMock.Setup(x => x.ValidateAsync(It.IsAny<TestRequest>()))
            .ReturnsAsync((true, null));

        var businessRules = new List<IBusinessRule<TestRequest>> { ruleMock.Object };
        var behavior = new BusinessValidationBehavior<TestRequest, string>(businessRules);
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
        var ruleMock = new Mock<IBusinessRule<TestRequest>>();
        ruleMock.Setup(x => x.ValidateAsync(It.IsAny<TestRequest>()))
            .ReturnsAsync((false, "Validation failed"));

        var businessRules = new List<IBusinessRule<TestRequest>> { ruleMock.Object };
        var behavior = new BusinessValidationBehavior<TestRequest, string>(businessRules);
        var nextMock = new Mock<RequestHandlerDelegate<string>>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            behavior.Handle(new TestRequest(), nextMock.Object, CancellationToken.None));

        Assert.Contains("Validation failed", exception.ValidationErrors);
        nextMock.Verify(x => x(), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCollectAllErrors_WhenMultipleValidationsFail()
    {
        // Arrange
        var rule1Mock = new Mock<IBusinessRule<TestRequest>>();
        rule1Mock.Setup(x => x.ValidateAsync(It.IsAny<TestRequest>()))
            .ReturnsAsync((false, "Error 1"));

        var rule2Mock = new Mock<IBusinessRule<TestRequest>>();
        rule2Mock.Setup(x => x.ValidateAsync(It.IsAny<TestRequest>()))
            .ReturnsAsync((false, "Error 2"));

        var businessRules = new List<IBusinessRule<TestRequest>> { rule1Mock.Object, rule2Mock.Object };
        var behavior = new BusinessValidationBehavior<TestRequest, string>(businessRules);
        var nextMock = new Mock<RequestHandlerDelegate<string>>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessValidationException>(() =>
            behavior.Handle(new TestRequest(), nextMock.Object, CancellationToken.None));

        Assert.Contains("Error 1", exception.ValidationErrors);
        Assert.Contains("Error 2", exception.ValidationErrors);
        nextMock.Verify(x => x(), Times.Never);
    }
}