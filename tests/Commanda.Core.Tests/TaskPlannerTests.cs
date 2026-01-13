using NUnit.Framework;
using Moq;
using Commanda.Core;

namespace Commanda.Core.Tests;

[TestFixture]
public class TaskPlannerTests
{
    private Mock<ILlmProviderManager> _llmManagerMock = null!;
    private TaskPlanner _taskPlanner = null!;

    [SetUp]
    public void Setup()
    {
        _llmManagerMock = new Mock<ILlmProviderManager>();
        _taskPlanner = new TaskPlanner(_llmManagerMock.Object);
    }

    [Test]
    public async Task GeneratePlanAsync_ValidUserInput_ReturnsExecutionPlan()
    {
        // Arrange
        var context = new AgentContext { UserInput = "ファイルをコピーしてください" };
        var mockProvider = new Mock<ILlmProvider>();
        var expectedJson = @"{
            ""description"": ""ファイルコピー計画"",
            ""steps"": [
                {
                    ""toolName"": ""copy_file"",
                    ""arguments"": { ""source"": ""file1.txt"", ""destination"": ""file2.txt"" },
                    ""expectedOutcome"": ""ファイルがコピーされる"",
                    ""timeout"": 30
                }
            ],
            ""parameters"": {}
        }";

        _llmManagerMock.Setup(m => m.GetActiveProviderAsync()).ReturnsAsync(mockProvider.Object);
        mockProvider.Setup(p => p.StreamResponseAsync(It.IsAny<string>(), It.IsAny<ResponseFormat>(), It.IsAny<CancellationToken>()))
                   .Returns(CreateAsyncEnumerable(expectedJson));

        // Act
        var result = await _taskPlanner.GeneratePlanAsync(context);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("ファイルコピー計画", result.Description);
        Assert.AreEqual(1, result.Steps.Count);
        Assert.AreEqual("copy_file", result.Steps[0].ToolName);
    }

    [Test]
    public async Task GeneratePlanAsync_WithFeedback_IncludesFeedbackInPrompt()
    {
        // Arrange
        var context = new AgentContext { UserInput = "ファイルをコピーしてください" };
        context.AddFeedback("以前の試行で失敗しました");
        var mockProvider = new Mock<ILlmProvider>();
        var expectedJson = @"{
            ""description"": ""修正されたファイルコピー計画"",
            ""steps"": [
                {
                    ""toolName"": ""copy_file"",
                    ""arguments"": { ""source"": ""file1.txt"", ""destination"": ""file2.txt"" },
                    ""expectedOutcome"": ""ファイルがコピーされる"",
                    ""timeout"": 30
                }
            ],
            ""parameters"": {}
        }";

        _llmManagerMock.Setup(m => m.GetActiveProviderAsync()).ReturnsAsync(mockProvider.Object);
        mockProvider.Setup(p => p.StreamResponseAsync(It.Is<string>(s => s.Contains("以前のフィードバック")), It.IsAny<ResponseFormat>(), It.IsAny<CancellationToken>()))
                   .Returns(CreateAsyncEnumerable(expectedJson));

        // Act
        var result = await _taskPlanner.GeneratePlanAsync(context);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("修正されたファイルコピー計画", result.Description);
    }

    [Test]
    public void GeneratePlanAsync_InvalidJson_ThrowsException()
    {
        // Arrange
        var context = new AgentContext { UserInput = "無効なリクエスト" };
        var mockProvider = new Mock<ILlmProvider>();
        var invalidJson = "{ invalid json }";

        _llmManagerMock.Setup(m => m.GetActiveProviderAsync()).ReturnsAsync(mockProvider.Object);
        mockProvider.Setup(p => p.StreamResponseAsync(It.IsAny<string>(), It.IsAny<ResponseFormat>(), It.IsAny<CancellationToken>()))
                   .Returns(CreateAsyncEnumerable(invalidJson));

        // Act & Assert
        Assert.ThrowsAsync<PlanningException>(async () => await _taskPlanner.GeneratePlanAsync(context));
    }

    private async IAsyncEnumerable<string> CreateAsyncEnumerable(string value)
    {
        yield return value;
    }
}