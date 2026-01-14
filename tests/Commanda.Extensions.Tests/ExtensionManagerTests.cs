using Xunit;
using Moq;
using Commanda.Core;
using Commanda.Extensions;
using System.Composition;
using System.Composition.Hosting;

namespace Commanda.Extensions.Tests;

public class ExtensionManagerTests
{
    private readonly ExtensionManager _extensionManager;
    private readonly Mock<IMcpExtension> _mockExtension;

    public ExtensionManagerTests()
    {
        _extensionManager = new ExtensionManager();
        _mockExtension = new Mock<IMcpExtension>();
        _mockExtension.Setup(e => e.Name).Returns("TestExtension");
        _mockExtension.Setup(e => e.Version).Returns("1.0.0");
        _mockExtension.Setup(e => e.ToolTypes).Returns(new[] { typeof(TestTool) });
    }

    [Fact]
    public async Task LoadExtensionsAsync_CompletesSuccessfully()
    {
        // Act
        await _extensionManager.LoadExtensionsAsync();

        // Assert
        // 現在の実装では何もロードしないので、例外が発生しないことを確認
        Assert.True(true);
    }

    [Fact]
    public async Task GetLoadedExtensionsAsync_InitiallyEmpty()
    {
        // Act
        var extensions = await _extensionManager.GetLoadedExtensionsAsync();

        // Assert
        Assert.Empty(extensions);
    }

    [Fact]
    public async Task RegisterExtensionAsync_NewExtension_ReturnsTrue()
    {
        // Act
        var result = await _extensionManager.RegisterExtensionAsync(_mockExtension.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RegisterExtensionAsync_ExistingExtension_ReturnsFalse()
    {
        // Arrange
        await _extensionManager.RegisterExtensionAsync(_mockExtension.Object);

        // Act
        var result = await _extensionManager.RegisterExtensionAsync(_mockExtension.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetLoadedExtensionsAsync_AfterRegistration_ContainsExtension()
    {
        // Arrange
        await _extensionManager.RegisterExtensionAsync(_mockExtension.Object);

        // Act
        var extensions = await _extensionManager.GetLoadedExtensionsAsync();

        // Assert
        Assert.Single(extensions);
        Assert.Equal("TestExtension", extensions.First().Name);
    }

    [Fact]
    public async Task UnregisterExtensionAsync_ExistingExtension_ReturnsTrue()
    {
        // Arrange
        await _extensionManager.RegisterExtensionAsync(_mockExtension.Object);

        // Act
        var result = await _extensionManager.UnregisterExtensionAsync("TestExtension");

        // Assert
        Assert.True(result);

        // Verify removed
        var extensions = await _extensionManager.GetLoadedExtensionsAsync();
        Assert.Empty(extensions);
    }

    [Fact]
    public async Task UnregisterExtensionAsync_NonExistingExtension_ReturnsFalse()
    {
        // Act
        var result = await _extensionManager.UnregisterExtensionAsync("NonExistingExtension");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ReloadExtensionsAsync_ClearsAndReloads()
    {
        // Arrange
        await _extensionManager.RegisterExtensionAsync(_mockExtension.Object);

        // Act
        await _extensionManager.ReloadExtensionsAsync();

        // Assert
        // リロードにより拡張機能がクリアされ、再ロードされる
        var extensions = await _extensionManager.GetLoadedExtensionsAsync();
        Assert.NotEmpty(extensions);
        Assert.Contains(extensions, e => e.Name == "TestMefExtension");
    }

    [Fact]
    public async Task LoadExtensionsWithMefAsync_LoadsExportedExtensions()
    {
        // Arrange - Create MEF container with test extension
        var configuration = new ContainerConfiguration()
            .WithPart<TestMefExtension>();

        using var container = configuration.CreateContainer();

        // Create extension manager that uses MEF (future implementation)
        // For now, this test will fail until MEF is implemented
        var extensionManager = new ExtensionManager();

        // Act
        await extensionManager.LoadExtensionsAsync();

        // Assert - Should load MEF exported extensions
        var extensions = await extensionManager.GetLoadedExtensionsAsync();
        Assert.Contains(extensions, e => e.Name == "TestMefExtension");
    }

    // MEF exported test extension
    [Export(typeof(IMcpExtension))]
    private class TestMefExtension : IMcpExtension
    {
        public string Name => "TestMefExtension";
        public string Version => "1.0.0";
        public IEnumerable<Type> ToolTypes => new[] { typeof(TestTool) };

        public Task InitializeAsync(IServiceProvider? services)
        {
            return Task.CompletedTask;
        }
    }

    // テスト用のツールクラス
    [McpServerToolTypeAttribute]
    private class TestTool
    {
        // テスト用のツール実装
    }
}