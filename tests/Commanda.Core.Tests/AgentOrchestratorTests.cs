using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Runtime.Versioning;

namespace Commanda.Core.Tests;

[SupportedOSPlatform("windows")]
public class AgentOrchestratorTests
{
    private readonly Mock<ITaskPlanner> _taskPlannerMock;
    private readonly Mock<IExecutionMonitor> _executionMonitorMock;
    private readonly Mock<IStateManager> _stateManagerMock;
    private readonly Mock<ILlmProviderManager> _llmManagerMock;
    private readonly Mock<IMcpServer> _mcpServerMock;
    private readonly InputValidator _inputValidator;
    private readonly Mock<ILogger<AgentOrchestrator>> _loggerMock;
    private readonly AgentOrchestrator _orchestrator;

    public AgentOrchestratorTests()
    {
        _taskPlannerMock = new Mock<ITaskPlanner>();
        _executionMonitorMock = new Mock<IExecutionMonitor>();
        _stateManagerMock = new Mock<IStateManager>();
        _llmManagerMock = new Mock<ILlmProviderManager>();
        _mcpServerMock = new Mock<IMcpServer>();
        _inputValidator = new InputValidator();
        _loggerMock = new Mock<ILogger<AgentOrchestrator>>();

        // Setup default behaviors to prevent null reference exceptions
        _executionMonitorMock.Setup(m => m.EvaluateResultAsync(It.IsAny<ExecutionResult>(), It.IsAny<AgentContext>()))
                             .ReturnsAsync(new EvaluationResult { IsSuccessful = false, ShouldRetry = false });
        _stateManagerMock.Setup(m => m.SaveStateAsync(It.IsAny<AgentContext>()))
                         .Returns(Task.CompletedTask);
        _mcpServerMock.Setup(m => m.ExecuteToolAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<TimeSpan>()))
                      .ReturnsAsync(new ToolResult { IsSuccessful = true, Output = "Mock output" });

        _orchestrator = new AgentOrchestrator(
            _taskPlannerMock.Object,
            _executionMonitorMock.Object,
            _stateManagerMock.Object,
            _llmManagerMock.Object,
            _mcpServerMock.Object,
            _inputValidator,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteTaskAsync_ValidInput_ReturnsSuccessfulResult()
    {
        // Arrange
        var userInput = "Hello, create a test file";
        var expectedContent = "Task completed successfully";

        // Setup task planner to return a simple plan
        var plan = new ExecutionPlan
        {
            Description = "Test plan",
            Steps = new List<ExecutionStep>
            {
                new ExecutionStep
                {
                    ToolName = "test_tool",
                    Arguments = new Dictionary<string, object> { { "input", "test" } },
                    ExpectedOutcome = "File created",
                    Timeout = TimeSpan.FromSeconds(30)
                }
            }
        };

        _taskPlannerMock.Setup(p => p.GeneratePlanAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(plan);

        // Setup MCP server to return successful tool result
        var toolResult = new ToolResult
        {
            IsSuccessful = true,
            Output = expectedContent,
            Duration = TimeSpan.FromSeconds(1)
        };

        _mcpServerMock.Setup(m => m.ExecuteToolAsync("test_tool", It.IsAny<Dictionary<string, object>>(), It.IsAny<TimeSpan>()))
                     .ReturnsAsync(toolResult);

        // Setup execution monitor to return successful evaluation
        var evaluationResult = new EvaluationResult
        {
            IsSuccessful = true,
            ShouldRetry = false,
            Feedback = "Task completed successfully"
        };

        _executionMonitorMock.Setup(m => m.EvaluateResultAsync(It.IsAny<ExecutionResult>(), It.IsAny<AgentContext>()))
                            .ReturnsAsync(evaluationResult);

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(userInput);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Contains("正常に完了", result.Content);
        Assert.Equal(1, result.StepsExecuted);
        Assert.True(result.Duration > TimeSpan.Zero);

        // Verify interactions
        _taskPlannerMock.Verify(p => p.GeneratePlanAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()), Times.Once);
        _mcpServerMock.Verify(m => m.ExecuteToolAsync("test_tool", It.IsAny<Dictionary<string, object>>(), It.IsAny<TimeSpan>()), Times.Once);
        _executionMonitorMock.Verify(m => m.EvaluateResultAsync(It.IsAny<ExecutionResult>(), It.IsAny<AgentContext>()), Times.Once);
        _stateManagerMock.Verify(m => m.SaveStateAsync(It.IsAny<AgentContext>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteTaskAsync_PlanningFails_ReturnsFailedResult()
    {
        // Arrange
        var userInput = "Test input";
        var expectedError = "Planning failed";

        // Setup task planner to throw exception
        _taskPlannerMock.Setup(p => p.GeneratePlanAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
                       .ThrowsAsync(new Exception(expectedError));

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(userInput);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Contains(expectedError, result.Content);
        Assert.Equal(0, result.StepsExecuted);
    }

    [Fact]
    public void GetCurrentStatus_NoExecution_ReturnsIdle()
    {
        // Act
        var status = _orchestrator.GetCurrentStatus();

        // Assert
        Assert.Equal(ExecutionStatus.Idle, status);
    }

    [Fact]
    public async Task ExecuteTaskAsync_RetryRequired_ContinuesExecution()
    {
        // Arrange
        var userInput = "Test input requiring retry";

        // Setup task planner to return a plan
        var plan = new ExecutionPlan
        {
            Description = "Retry test plan",
            Steps = new List<ExecutionStep>
            {
                new ExecutionStep
                {
                    ToolName = "test_tool",
                    Arguments = new Dictionary<string, object> { { "input", "test" } },
                    ExpectedOutcome = "File created",
                    Timeout = TimeSpan.FromSeconds(30)
                }
            }
        };

        _taskPlannerMock.Setup(p => p.GeneratePlanAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(plan);

        // Setup MCP server to return successful tool result
        var toolResult = new ToolResult
        {
            IsSuccessful = true,
            Output = "Initial result",
            Duration = TimeSpan.FromSeconds(1)
        };

        _mcpServerMock.Setup(m => m.ExecuteToolAsync("test_tool", It.IsAny<Dictionary<string, object>>(), It.IsAny<TimeSpan>()))
                     .ReturnsAsync(toolResult);

        // Setup execution monitor to require retry first, then succeed
        var retryEvaluation = new EvaluationResult
        {
            IsSuccessful = false,
            ShouldRetry = true,
            Feedback = "Need to retry",
            Reason = "Incomplete"
        };

        var successEvaluation = new EvaluationResult
        {
            IsSuccessful = true,
            ShouldRetry = false,
            Feedback = "Task completed successfully"
        };

        _executionMonitorMock.SetupSequence(m => m.EvaluateResultAsync(It.IsAny<ExecutionResult>(), It.IsAny<AgentContext>()))
                            .ReturnsAsync(retryEvaluation)
                            .ReturnsAsync(successEvaluation);

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(userInput);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(2, result.StepsExecuted); // Executed twice due to retry

        // Verify interactions
        _taskPlannerMock.Verify(p => p.GeneratePlanAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _executionMonitorMock.Verify(m => m.EvaluateResultAsync(It.IsAny<ExecutionResult>(), It.IsAny<AgentContext>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteTaskAsync_CancellationRequested_IsHandled()
    {
        // Arrange
        var userInput = "Test input";

        // Setup task planner to return a plan that will be cancelled
        var plan = new ExecutionPlan
        {
            Description = "Cancellation test plan",
            Steps = new List<ExecutionStep>
            {
                new ExecutionStep
                {
                    ToolName = "test_tool",
                    Arguments = new Dictionary<string, object> { { "input", "test" } },
                    ExpectedOutcome = "File created",
                    Timeout = TimeSpan.FromSeconds(30)
                }
            }
        };

        _taskPlannerMock.Setup(p => p.GeneratePlanAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(plan);

        // Setup MCP server to throw OperationCanceledException
        _mcpServerMock.Setup(m => m.ExecuteToolAsync("test_tool", It.IsAny<Dictionary<string, object>>(), It.IsAny<TimeSpan>()))
                     .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(userInput);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Contains("キャンセル", result.Content);
    }
}
