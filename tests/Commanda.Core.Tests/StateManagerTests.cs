using NUnit.Framework;
using Commanda.Core;
using Moq;
using Microsoft.Extensions.Logging;

namespace Commanda.Core.Tests;

[TestFixture]
public class StateManagerTests
{
    private Mock<ILogger<StateManager>> _loggerMock = null!;
    private StateManager _stateManager = null!;
    private string _testStateDirectory = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<StateManager>>();
        _testStateDirectory = Path.Combine(Path.GetTempPath(), "CommandaTest", "State");
        _stateManager = new StateManager(_loggerMock.Object, _testStateDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_testStateDirectory))
        {
            Directory.Delete(_testStateDirectory, true);
        }
    }

    [Test]
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
        Assert.AreEqual(1, files.Length);

        var fileContent = await File.ReadAllTextAsync(files[0]);
        Assert.That(fileContent, Does.Contain("Test input"));
        Assert.That(fileContent, Does.Contain("\"Status\":1"));
    }

    [Test]
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
        Assert.IsNotNull(loadedContext);
        Assert.AreEqual("Load test", loadedContext!.UserInput);
        Assert.AreEqual(ExecutionStatus.Completed, loadedContext!.Status);
        Assert.IsTrue(loadedContext!.IsCompleted);
    }

    [Test]
    public async Task LoadStateAsync_NonExistentSessionId_ReturnsNull()
    {
        // Act
        var result = await _stateManager.LoadStateAsync("nonexistent");

        // Assert
        Assert.IsNull(result);
    }

    [Test]
    public async Task ClearStateAsync_ExistingSessionId_RemovesStateFile()
    {
        // Arrange
        var context = new AgentContext { UserInput = "Clear test" };
        await _stateManager.SaveStateAsync(context);
        var sessionId = GenerateExpectedSessionId(context);

        // Verify file exists
        var filesBefore = Directory.GetFiles(_testStateDirectory, "*.state.json");
        Assert.AreEqual(1, filesBefore.Length);

        // Act
        await _stateManager.ClearStateAsync(sessionId);

        // Assert
        var filesAfter = Directory.GetFiles(_testStateDirectory, "*.state.json");
        Assert.AreEqual(0, filesAfter.Length);
    }

    private string GenerateExpectedSessionId(AgentContext context)
    {
        var input = context.UserInput ?? "unknown";
        var time = context.StartedAt.ToString("yyyyMMddHHmmss");
        var hash = input.GetHashCode().ToString("X8");
        return $"{time}_{hash}";
    }
}
