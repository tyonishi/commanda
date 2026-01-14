using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Commanda.Core;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Commanda.Core.Tests;

/// <summary>
/// エージェント実行フローの統合テスト
/// </summary>
public class AgentExecutionIntegrationTests
{
    private readonly Mock<ITaskPlanner> _taskPlannerMock;
    private readonly Mock<IExecutionMonitor> _executionMonitorMock;
    private readonly Mock<IStateManager> _stateManagerMock;
    private readonly Mock<ILlmProviderManager> _llmManagerMock;
    private readonly Mock<IMcpServer> _mcpServerMock;
    private readonly InputValidator _inputValidator;
    private readonly Mock<ILogger<AgentOrchestrator>> _loggerMock;
    private readonly AgentOrchestrator _orchestrator;

    public AgentExecutionIntegrationTests()
    {
        _taskPlannerMock = new Mock<ITaskPlanner>();
        _executionMonitorMock = new Mock<IExecutionMonitor>();
        _stateManagerMock = new Mock<IStateManager>();
        _llmManagerMock = new Mock<ILlmProviderManager>();
        _mcpServerMock = new Mock<IMcpServer>();
        _inputValidator = new InputValidator();
        _loggerMock = new Mock<ILogger<AgentOrchestrator>>();

        // Setup default behaviors
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
    public async Task FullExecutionFlow_FileOperationScenario_Succeeds()
    {
        // Arrange
        var userInput = "test.txtというファイルを作成して「Hello World」と書き込んでください";

        // Setup task planner to return file creation plan
        var plan = new ExecutionPlan
        {
            Description = "ファイル作成と書き込み",
            Steps = new List<ExecutionStep>
            {
                new ExecutionStep
                {
                    ToolName = "write_file",
                    Arguments = new Dictionary<string, object>
                    {
                        { "path", "test.txt" },
                        { "content", "Hello World" }
                    },
                    ExpectedOutcome = "ファイルが作成され内容が書き込まれる",
                    Timeout = TimeSpan.FromSeconds(30)
                }
            }
        };

        _taskPlannerMock.Setup(p => p.GeneratePlanAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(plan);

        // Setup MCP server to simulate file writing
        var toolResult = new ToolResult
        {
            IsSuccessful = true,
            Output = "ファイルに書き込みました",
            Duration = TimeSpan.FromMilliseconds(100)
        };

        _mcpServerMock.Setup(m => m.ExecuteToolAsync("write_file", It.IsAny<Dictionary<string, object>>(), It.IsAny<TimeSpan>()))
                     .ReturnsAsync(toolResult);

        // Setup execution monitor to return successful evaluation
        var evaluationResult = new EvaluationResult
        {
            IsSuccessful = true,
            ShouldRetry = false,
            Feedback = "ファイル操作が正常に完了しました"
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
        _mcpServerMock.Verify(m => m.ExecuteToolAsync("write_file", It.IsAny<Dictionary<string, object>>(), It.IsAny<TimeSpan>()), Times.Once);
        _executionMonitorMock.Verify(m => m.EvaluateResultAsync(It.IsAny<ExecutionResult>(), It.IsAny<AgentContext>()), Times.Once);
        _stateManagerMock.Verify(m => m.SaveStateAsync(It.IsAny<AgentContext>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task FullExecutionFlow_MultiStepScenario_Succeeds()
    {
        // Arrange
        var userInput = "ディレクトリを作成して、その中にファイルをコピーしてください";

        // Setup task planner to return multi-step plan
        var plan = new ExecutionPlan
        {
            Description = "ディレクトリ作成とファイルコピー",
            Steps = new List<ExecutionStep>
            {
                new ExecutionStep
                {
                    ToolName = "create_directory",
                    Arguments = new Dictionary<string, object> { { "path", "test_dir" } },
                    ExpectedOutcome = "ディレクトリが作成される",
                    Timeout = TimeSpan.FromSeconds(30)
                },
                new ExecutionStep
                {
                    ToolName = "copy_file",
                    Arguments = new Dictionary<string, object>
                    {
                        { "source", "source.txt" },
                        { "destination", "test_dir/dest.txt" }
                    },
                    ExpectedOutcome = "ファイルがコピーされる",
                    Timeout = TimeSpan.FromSeconds(30)
                }
            }
        };

        _taskPlannerMock.Setup(p => p.GeneratePlanAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(plan);

        // Setup MCP server for both operations
        var dirResult = new ToolResult
        {
            IsSuccessful = true,
            Output = "ディレクトリを作成しました",
            Duration = TimeSpan.FromMilliseconds(50)
        };

        var copyResult = new ToolResult
        {
            IsSuccessful = true,
            Output = "ファイルをコピーしました",
            Duration = TimeSpan.FromMilliseconds(150)
        };

        _mcpServerMock.SetupSequence(m => m.ExecuteToolAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<TimeSpan>()))
                     .ReturnsAsync(dirResult)
                     .ReturnsAsync(copyResult);

        // Setup execution monitor to return successful evaluation
        var evaluationResult = new EvaluationResult
        {
            IsSuccessful = true,
            ShouldRetry = false,
            Feedback = "マルチステップ操作が正常に完了しました"
        };

        _executionMonitorMock.Setup(m => m.EvaluateResultAsync(It.IsAny<ExecutionResult>(), It.IsAny<AgentContext>()))
                            .ReturnsAsync(evaluationResult);

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(userInput);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Contains("正常に完了", result.Content);
        Assert.Equal(2, result.StepsExecuted);
        Assert.True(result.Duration > TimeSpan.Zero); // Execution took some time

        // Verify interactions
        _taskPlannerMock.Verify(p => p.GeneratePlanAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()), Times.Once);
        _mcpServerMock.Verify(m => m.ExecuteToolAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<TimeSpan>()), Times.Exactly(2));
        _executionMonitorMock.Verify(m => m.EvaluateResultAsync(It.IsAny<ExecutionResult>(), It.IsAny<AgentContext>()), Times.Once);
    }

    [Fact]
    public async Task FullExecutionFlow_RetryScenario_Succeeds()
    {
        // Arrange
        var userInput = "ファイル操作を実行してください";

        // Setup task planner to return plan
        var plan = new ExecutionPlan
        {
            Description = "ファイル操作",
            Steps = new List<ExecutionStep>
            {
                new ExecutionStep
                {
                    ToolName = "file_operation",
                    Arguments = new Dictionary<string, object> { { "action", "create" } },
                    ExpectedOutcome = "ファイルが作成される",
                    Timeout = TimeSpan.FromSeconds(30)
                }
            }
        };

        _taskPlannerMock.Setup(p => p.GeneratePlanAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(plan);

        // Setup MCP server to succeed on retry
        var failureResult = new ToolResult
        {
            IsSuccessful = false,
            Output = null,
            Error = "一時的なエラー",
            Duration = TimeSpan.FromMilliseconds(100)
        };

        var successResult = new ToolResult
        {
            IsSuccessful = true,
            Output = "ファイルが作成されました",
            Duration = TimeSpan.FromMilliseconds(100)
        };

        _mcpServerMock.SetupSequence(m => m.ExecuteToolAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<TimeSpan>()))
                     .ReturnsAsync(failureResult)
                     .ReturnsAsync(successResult);

        // Setup execution monitor to require retry then succeed
        var retryEvaluation = new EvaluationResult
        {
            IsSuccessful = false,
            ShouldRetry = true,
            Feedback = "リトライが必要です",
            Reason = "一時的なエラー"
        };

        var successEvaluation = new EvaluationResult
        {
            IsSuccessful = true,
            ShouldRetry = false,
            Feedback = "操作が成功しました"
        };

        _executionMonitorMock.SetupSequence(m => m.EvaluateResultAsync(It.IsAny<ExecutionResult>(), It.IsAny<AgentContext>()))
                            .ReturnsAsync(retryEvaluation)
                            .ReturnsAsync(successEvaluation);

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(userInput);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Contains("正常に完了", result.Content);
        Assert.Equal(2, result.StepsExecuted); // Executed twice due to retry

        // Verify interactions
        _taskPlannerMock.Verify(p => p.GeneratePlanAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mcpServerMock.Verify(m => m.ExecuteToolAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<TimeSpan>()), Times.Exactly(2));
        _executionMonitorMock.Verify(m => m.EvaluateResultAsync(It.IsAny<ExecutionResult>(), It.IsAny<AgentContext>()), Times.Exactly(2));
    }

    [Fact]
    public async Task FullExecutionFlow_InvalidInput_Rejected()
    {
        // Arrange
        var invalidInput = ""; // Empty input

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(invalidInput);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Contains("入力検証エラー", result.Content);
        Assert.Equal(0, result.StepsExecuted);

        // Verify no execution occurred
        _taskPlannerMock.Verify(p => p.GeneratePlanAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()), Times.Never);
        _mcpServerMock.Verify(m => m.ExecuteToolAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<TimeSpan>()), Times.Never);
    }
}