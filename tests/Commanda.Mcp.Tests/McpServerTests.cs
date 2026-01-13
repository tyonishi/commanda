using NUnit.Framework;
using Moq;
using Commanda.Core;
using Commanda.Mcp;
using Commanda.Extensions;

namespace Commanda.Mcp.Tests;

[TestFixture]
public class McpServerTests
{
    private Mock<IExtensionManager> _extensionManagerMock = null!;
    private McpServer _mcpServer = null!;

    [SetUp]
    public void Setup()
    {
        _extensionManagerMock = new Mock<IExtensionManager>();
        _mcpServer = new McpServer(_extensionManagerMock.Object);
    }

    [Test]
    public async Task InitializeAsync_LoadsExtensions()
    {
        // Arrange
        _extensionManagerMock.Setup(m => m.LoadExtensionsAsync())
                            .Returns(Task.CompletedTask);

        // Act
        await _mcpServer.InitializeAsync();

        // Assert
        _extensionManagerMock.Verify(m => m.LoadExtensionsAsync(), Times.Once);
    }

    [Test]
    public async Task GetAvailableToolsAsync_ReturnsBuiltInTools()
    {
        // Arrange
        await _mcpServer.InitializeAsync();

        // Act
        var tools = await _mcpServer.GetAvailableToolsAsync();

        // Assert
        Assert.That(tools, Does.Contain("read_file"));
        Assert.That(tools, Does.Contain("write_file"));
        Assert.That(tools, Does.Contain("list_directory"));
    }

    [Test]
    public async Task ExecuteToolAsync_ReadFile_Success()
    {
        // Arrange
        await _mcpServer.InitializeAsync();
        var testFile = Path.GetTempFileName();
        var testContent = "Test file content";
        await File.WriteAllTextAsync(testFile, testContent);

        var arguments = new Dictionary<string, object>
        {
            ["path"] = testFile
        };

        // Act
        var result = await _mcpServer.ExecuteToolAsync("read_file", arguments, TimeSpan.FromSeconds(30));

        // Assert
        Assert.That(result.IsSuccessful, Is.True);
        Assert.That(result.Output, Is.EqualTo(testContent));

        // Cleanup
        File.Delete(testFile);
    }

    [Test]
    public async Task ExecuteToolAsync_ReadFile_FileNotFound_ReturnsError()
    {
        // Arrange
        await _mcpServer.InitializeAsync();
        var arguments = new Dictionary<string, object>
        {
            ["path"] = "nonexistent_file.txt"
        };

        // Act
        var result = await _mcpServer.ExecuteToolAsync("read_file", arguments, TimeSpan.FromSeconds(30));

        // Assert
        Assert.That(result.IsSuccessful, Is.False);
        Assert.That(result.Error, Does.Contain("ファイル読み込みエラー"));
    }

    [Test]
    public async Task ExecuteToolAsync_WriteFile_Success()
    {
        // Arrange
        await _mcpServer.InitializeAsync();
        var testFile = Path.GetTempFileName();
        var testContent = "Test content to write";

        var arguments = new Dictionary<string, object>
        {
            ["path"] = testFile,
            ["content"] = testContent
        };

        // Act
        var result = await _mcpServer.ExecuteToolAsync("write_file", arguments, TimeSpan.FromSeconds(30));

        // Assert
        Assert.That(result.IsSuccessful, Is.True);
        Assert.That(result.Output, Is.EqualTo("ファイルが正常に書き込まれました"));

        // Verify file content
        var actualContent = await File.ReadAllTextAsync(testFile);
        Assert.That(actualContent, Is.EqualTo(testContent));

        // Cleanup
        File.Delete(testFile);
    }

    [Test]
    public async Task ExecuteToolAsync_WriteFile_MissingParameters_ReturnsError()
    {
        // Arrange
        await _mcpServer.InitializeAsync();
        var arguments = new Dictionary<string, object>(); // Missing path and content

        // Act
        var result = await _mcpServer.ExecuteToolAsync("write_file", arguments, TimeSpan.FromSeconds(30));

        // Assert
        Assert.That(result.IsSuccessful, Is.False);
        Assert.That(result.Error, Does.Contain("pathパラメータが必要です"));
    }

    [Test]
    public async Task ExecuteToolAsync_UnknownTool_ReturnsError()
    {
        // Arrange
        await _mcpServer.InitializeAsync();
        var arguments = new Dictionary<string, object>();

        // Act
        var result = await _mcpServer.ExecuteToolAsync("unknown_tool", arguments, TimeSpan.FromSeconds(30));

        // Assert
        Assert.That(result.IsSuccessful, Is.False);
        Assert.That(result.Error, Does.Contain("見つかりません"));
    }
}