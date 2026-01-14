using Xunit;
using Commanda.Core;
using Moq;
using Microsoft.Extensions.Logging;

namespace Commanda.Core.Tests;

public class StateManagerTests : IDisposable
{
    private readonly Mock<ILogger<StateManager>> _loggerMock;
    private readonly StateManager _stateManager;
    private readonly string _testStateDirectory;

    public StateManagerTests()
    {
        _loggerMock = new Mock<ILogger<StateManager>>();
        _testStateDirectory = Path.Combine(Path.GetTempPath(), "CommandaTest", $"State_{Guid.NewGuid()}");
        _stateManager = new StateManager(_loggerMock.Object, _testStateDirectory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testStateDirectory))
            {
                Directory.Delete(_testStateDirectory, true);
            }
        }
        catch
        {
            // クリーンアップエラーは無視
        }
    }

    [Fact]
    public async Task SaveStateAsync_ValidContext_SavesStateToFile()
    {
        // Arrange
        var context = new AgentContext
        {
            UserInput = "Test input",
            Status = ExecutionStatus.Planning,
            CurrentPlan = new ExecutionPlan { Description = "Test plan" },
            IsCompleted = false
        };

        // Act
        await _stateManager.SaveStateAsync(context);

        // Assert
        var files = Directory.GetFiles(_testStateDirectory, "*.state.json");
        Assert.Single(files);

        var fileContent = await File.ReadAllTextAsync(files[0]);
        Assert.Contains("Test input", fileContent);
        Assert.Contains("Planning", fileContent);
    }

    [Fact]
    public async Task LoadStateAsync_ExistingSessionId_ReturnsContext()
    {
        // Arrange
        var context = new AgentContext
        {
            UserInput = "Load test",
            Status = ExecutionStatus.Completed,
            IsCompleted = true
        };

        await _stateManager.SaveStateAsync(context);
        var sessionId = GenerateExpectedSessionId(context);

        // Act
        var loadedContext = await _stateManager.LoadStateAsync(sessionId);

        // Assert
        Assert.NotNull(loadedContext);
        Assert.Equal("Load test", loadedContext.UserInput);
        Assert.Equal(ExecutionStatus.Completed, loadedContext.Status);
        Assert.True(loadedContext.IsCompleted);
    }

    [Fact]
    public async Task LoadStateAsync_NonExistentSessionId_ReturnsNull()
    {
        // Act
        var result = await _stateManager.LoadStateAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ClearStateAsync_ExistingSessionId_RemovesStateFile()
    {
        // Arrange
        var context = new AgentContext { UserInput = "Clear test" };
        await _stateManager.SaveStateAsync(context);
        var sessionId = GenerateExpectedSessionId(context);

        // Verify file exists
        var filesBefore = Directory.GetFiles(_testStateDirectory, "*.state.json");
        Assert.Single(filesBefore);

        // Act
        await _stateManager.ClearStateAsync(sessionId);

        // Assert
        var filesAfter = Directory.GetFiles(_testStateDirectory, "*.state.json");
        Assert.Empty(filesAfter);
    }

    private string GenerateExpectedSessionId(AgentContext context)
    {
        var input = context.UserInput ?? "unknown";
        var time = context.StartedAt.ToString("yyyyMMddHHmmss");
        var hash = input.GetHashCode().ToString("X8");
        return $"{time}_{hash}";
    }
}