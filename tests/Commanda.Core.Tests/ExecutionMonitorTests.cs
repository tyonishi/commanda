using NUnit.Framework;
using Moq;
using Commanda.Core;
using Microsoft.Extensions.Logging;

namespace Commanda.Core.Tests;

[TestFixture]
public class ExecutionMonitorTests
{
    private Mock<ILogger<ExecutionMonitor>> _loggerMock = null!;
    private ExecutionMonitor _monitor = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ExecutionMonitor>>();
        _monitor = new ExecutionMonitor(_loggerMock.Object);
    }

    [Test]
    public async Task EvaluateResultAsync_SuccessfulResult_ReturnsSuccessfulEvaluation()
    {
        // Arrange
        var context = new AgentContext();
        var result = new ExecutionResult { IsSuccessful = true, Duration = TimeSpan.FromSeconds(1) };

        // Act
        var evaluation = await _monitor.EvaluateResultAsync(result, context);

        // Assert
        Assert.IsTrue(evaluation.IsSuccessful);
        Assert.AreEqual("実行が正常に完了しました", evaluation.Reason);
        Assert.IsFalse(evaluation.ShouldRetry);
    }

    [Test]
    public async Task EvaluateResultAsync_FailedResultWithRetryableError_ReturnsRetryEvaluation()
    {
        // Arrange
        var context = new AgentContext();
        var result = new ExecutionResult
        {
            IsSuccessful = false,
            Error = "Network timeout occurred",
            Duration = TimeSpan.FromSeconds(1)
        };

        // Act
        var evaluation = await _monitor.EvaluateResultAsync(result, context);

        // Assert
        Assert.IsFalse(evaluation.IsSuccessful);
        Assert.IsTrue(evaluation.ShouldRetry);
        Assert.That(evaluation.Feedback, Does.Contain("リトライ"));
    }

    [Test]
    public async Task EvaluateResultAsync_LongExecutionTime_AddsPerformanceFeedback()
    {
        // Arrange
        var context = new AgentContext();
        var result = new ExecutionResult
        {
            IsSuccessful = true,
            Duration = TimeSpan.FromMinutes(10) // Very long execution
        };

        // Act
        var evaluation = await _monitor.EvaluateResultAsync(result, context);

        // Assert
        Assert.IsTrue(evaluation.IsSuccessful);
        Assert.That(evaluation.Feedback, Does.Contain("実行時間が長くなっています"));
    }
}
