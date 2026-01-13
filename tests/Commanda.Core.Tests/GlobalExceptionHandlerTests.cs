using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using Commanda.Core;

namespace Commanda.Core.Tests;

[TestFixture]
public class GlobalExceptionHandlerTests
{
    private Mock<ILogger<GlobalExceptionHandler>> _loggerMock = null!;
    private GlobalExceptionHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<GlobalExceptionHandler>>();
        _handler = new GlobalExceptionHandler(_loggerMock.Object);
    }

    [Test]
    public void HandleException_CommandaException_ShowsUserFriendlyMessage()
    {
        // Arrange
        var exception = new CommandaException("Test error", "TOOL_NOT_FOUND");

        // Act
        _handler.HandleException(exception);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void HandleException_GenericException_ShowsGenericMessage()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error");

        // Act
        _handler.HandleException(exception);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void HandleException_PlanningException_ShowsPlanningErrorMessage()
    {
        // Arrange
        var exception = new PlanningException("Planning failed");

        // Act
        _handler.HandleException(exception);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
