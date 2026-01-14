using Xunit;
using Moq;
using Commanda.Core;
using Commanda.Mcp;
using Commanda.Extensions;

namespace Commanda.Mcp.Tests;

public class McpServerTests : IDisposable
{
    private readonly Mock<IExtensionManager> _extensionManagerMock;
    private readonly McpServer _mcpServer;
    private readonly List<string> _tempFiles = new();

    public McpServerTests()
    {
        _extensionManagerMock = new Mock<IExtensionManager>();
        _mcpServer = new McpServer(_extensionManagerMock.Object);
    }

    public void Dispose()
    {
        // Cleanup temporary files
        foreach (var file in _tempFiles)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        _tempFiles.Clear();
    }

    [Fact]
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

    [Fact]
    public async Task GetAvailableToolsAsync_ReturnsBuiltInTools()
    {
        // Arrange
        await _mcpServer.InitializeAsync();

        // Act
        var tools = await _mcpServer.GetAvailableToolsAsync();

        // Assert
        Assert.Contains("read_file", tools);
        Assert.Contains("write_file", tools);
        Assert.Contains("list_directory", tools);
    }

    [Fact]
    public async Task ExecuteToolAsync_ReadFile_Success()
    {
        // Arrange
        await _mcpServer.InitializeAsync();
        var testFile = Path.GetTempFileName();
        _tempFiles.Add(testFile);
        var testContent = "Test file content";
        await File.WriteAllTextAsync(testFile, testContent);

        var arguments = new Dictionary<string, object>
        {
            ["path"] = testFile
        };

        // Act
        var result = await _mcpServer.ExecuteToolAsync("read_file", arguments, TimeSpan.FromSeconds(30));

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(testContent, result.Output);
    }

    [Fact]
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
        Assert.False(result.IsSuccessful);
        Assert.Contains("ファイル読み込みエラー", result.Error);
    }

    [Fact]
    public async Task ExecuteToolAsync_WriteFile_Success()
    {
        // Arrange
        await _mcpServer.InitializeAsync();
        var testFile = Path.GetTempFileName();
        _tempFiles.Add(testFile);
        var testContent = "Test content to write";

        var arguments = new Dictionary<string, object>
        {
            ["path"] = testFile,
            ["content"] = testContent
        };

        // Act
        var result = await _mcpServer.ExecuteToolAsync("write_file", arguments, TimeSpan.FromSeconds(30));

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal("ファイルが正常に書き込まれました", result.Output);

        // Verify file content
        var actualContent = await File.ReadAllTextAsync(testFile);
        Assert.Equal(testContent, actualContent);
    }

    [Fact]
    public async Task ExecuteToolAsync_WriteFile_MissingParameters_ReturnsError()
    {
        // Arrange
        await _mcpServer.InitializeAsync();
        var arguments = new Dictionary<string, object>(); // Missing path and content

        // Act
        var result = await _mcpServer.ExecuteToolAsync("write_file", arguments, TimeSpan.FromSeconds(30));

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Contains("pathパラメータが必要です", result.Error);
    }

    [Fact]
    public async Task ExecuteToolAsync_UnknownTool_ReturnsError()
    {
        // Arrange
        await _mcpServer.InitializeAsync();
        var arguments = new Dictionary<string, object>();

        // Act
        var result = await _mcpServer.ExecuteToolAsync("unknown_tool", arguments, TimeSpan.FromSeconds(30));

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Contains("見つかりません", result.Error);
    }
}