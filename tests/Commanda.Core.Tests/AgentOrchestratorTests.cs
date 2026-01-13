using Moq;
using NUnit.Framework;

namespace Commanda.Core.Tests;

[TestFixture]
public class AgentOrchestratorTests
{
    private Mock<ILlmProviderManager> _llmManagerMock = null!;
    private Mock<IMcpServer> _mcpServerMock = null!;
    private InputValidator _inputValidator = null!;
    private AgentOrchestrator _orchestrator = null!;

    [SetUp]
    public void Setup()
    {
        _llmManagerMock = new Mock<ILlmProviderManager>();
        _mcpServerMock = new Mock<IMcpServer>();
        _inputValidator = new InputValidator();
        _orchestrator = new AgentOrchestrator(_llmManagerMock.Object, _mcpServerMock.Object, _inputValidator);
    }

    [Test]
    public async Task ExecuteTaskAsync_ValidInput_ReturnsSuccessfulResult()
    {
        // Arrange
        var userInput = "Hello, create a test file";
        var expectedResponse = "Task completed successfully";

        var mockProvider = new Mock<ILlmProvider>();
        mockProvider.Setup(p => p.GetResponseAsync(It.IsAny<string>(), It.IsAny<ResponseFormat>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(expectedResponse);

        _llmManagerMock.Setup(m => m.GetActiveProviderAsync())
                      .ReturnsAsync(mockProvider.Object);

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(userInput);

        // Assert
        Assert.That(result.IsSuccessful, Is.True);
        Assert.That(result.Content, Is.EqualTo(expectedResponse));
        Assert.That(result.StepsExecuted, Is.EqualTo(1));
        Assert.That(result.Duration, Is.GreaterThan(TimeSpan.Zero));
    }

    [Test]
    public async Task ExecuteTaskAsync_LlmProviderThrowsException_ReturnsFailedResult()
    {
        // Arrange
        var userInput = "Test input";
        var expectedError = "LLM provider error";

        var mockProvider = new Mock<ILlmProvider>();
        mockProvider.Setup(p => p.GetResponseAsync(It.IsAny<string>(), It.IsAny<ResponseFormat>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new Exception(expectedError));

        _llmManagerMock.Setup(m => m.GetActiveProviderAsync())
                      .ReturnsAsync(mockProvider.Object);

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(userInput);

        // Assert
        Assert.That(result.IsSuccessful, Is.False);
        Assert.That(result.Content, Does.Contain(expectedError));
        Assert.That(result.StepsExecuted, Is.EqualTo(0));
    }

    [Test]
    public void GetCurrentStatus_NoExecution_ReturnsIdle()
    {
        // Act
        var status = _orchestrator.GetCurrentStatus();

        // Assert
        Assert.That(status, Is.EqualTo(ExecutionStatus.Idle));
    }

    [Test]
    public async Task CancelExecutionAsync_CancellationRequested_IsHandled()
    {
        // Arrange
        var mockProvider = new Mock<ILlmProvider>();
        mockProvider.Setup(p => p.GetResponseAsync(It.IsAny<string>(), It.IsAny<ResponseFormat>(), It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new OperationCanceledException());

        _llmManagerMock.Setup(m => m.GetActiveProviderAsync())
                      .ReturnsAsync(mockProvider.Object);

        // Act
        var result = await _orchestrator.ExecuteTaskAsync("Test input");

        // Assert
        Assert.That(result.IsSuccessful, Is.False);
        Assert.That(result.Content, Does.Contain("キャンセル"));
    }
}
