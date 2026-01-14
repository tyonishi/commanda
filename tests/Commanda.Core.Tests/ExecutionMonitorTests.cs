using Xunit;
using Moq;
using Commanda.Core;
using Microsoft.Extensions.Logging;

namespace Commanda.Core.Tests;

public class ExecutionMonitorTests
{
    private readonly Mock<ILogger<ExecutionMonitor>> _loggerMock;
    private readonly ExecutionMonitor _monitor;

    public ExecutionMonitorTests()
    {
        _loggerMock = new Mock<ILogger<ExecutionMonitor>>();
        _monitor = new ExecutionMonitor(_loggerMock.Object);
    }

    [Fact]
    public async Task EvaluateResultAsync_SuccessfulResult_ReturnsSuccessfulEvaluation()
    {
        // Arrange
        var context = new AgentContext();
        var result = new ExecutionResult { IsSuccessful = true, Duration = TimeSpan.FromSeconds(1) };

        // Act
        var evaluation = await _monitor.EvaluateResultAsync(result, context);

        // Assert
        Assert.True(evaluation.IsSuccessful);
        Assert.Equal("実行が正常に完了しました", evaluation.Reason);
        Assert.False(evaluation.ShouldRetry);
    }

    [Fact]
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
        Assert.False(evaluation.IsSuccessful);
        Assert.True(evaluation.ShouldRetry);
        Assert.Contains("リトライ", evaluation.Feedback);
    }

    [Fact]
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
        Assert.True(evaluation.IsSuccessful);
        Assert.Contains("実行時間が長くなっています", evaluation.Feedback);
    }
}