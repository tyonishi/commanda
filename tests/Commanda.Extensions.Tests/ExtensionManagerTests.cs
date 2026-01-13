using NUnit.Framework;
using Moq;
using Commanda.Core;
using Commanda.Extensions;

namespace Commanda.Extensions.Tests;

[TestFixture]
public class ExtensionManagerTests
{
    private ExtensionManager _extensionManager = null!;
    private Mock<IMcpExtension> _mockExtension = null!;

    [SetUp]
    public void Setup()
    {
        _extensionManager = new ExtensionManager();
        _mockExtension = new Mock<IMcpExtension>();
        _mockExtension.Setup(e => e.Name).Returns("TestExtension");
        _mockExtension.Setup(e => e.Version).Returns("1.0.0");
        _mockExtension.Setup(e => e.ToolTypes).Returns(new[] { typeof(TestTool) });
    }

    [Test]
    public async Task LoadExtensionsAsync_CompletesSuccessfully()
    {
        // Act
        await _extensionManager.LoadExtensionsAsync();

        // Assert
        // 現在の実装では何もロードしないので、例外が発生しないことを確認
        Assert.Pass();
    }

    [Test]
    public async Task GetLoadedExtensionsAsync_InitiallyEmpty()
    {
        // Act
        var extensions = await _extensionManager.GetLoadedExtensionsAsync();

        // Assert
        Assert.That(extensions, Is.Empty);
    }

    [Test]
    public async Task RegisterExtensionAsync_NewExtension_ReturnsTrue()
    {
        // Act
        var result = await _extensionManager.RegisterExtensionAsync(_mockExtension.Object);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task RegisterExtensionAsync_ExistingExtension_ReturnsFalse()
    {
        // Arrange
        await _extensionManager.RegisterExtensionAsync(_mockExtension.Object);

        // Act
        var result = await _extensionManager.RegisterExtensionAsync(_mockExtension.Object);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task GetLoadedExtensionsAsync_AfterRegistration_ContainsExtension()
    {
        // Arrange
        await _extensionManager.RegisterExtensionAsync(_mockExtension.Object);

        // Act
        var extensions = await _extensionManager.GetLoadedExtensionsAsync();

        // Assert
        Assert.That(extensions.Count(), Is.EqualTo(1));
        Assert.That(extensions.First().Name, Is.EqualTo("TestExtension"));
    }

    [Test]
    public async Task UnregisterExtensionAsync_ExistingExtension_ReturnsTrue()
    {
        // Arrange
        await _extensionManager.RegisterExtensionAsync(_mockExtension.Object);

        // Act
        var result = await _extensionManager.UnregisterExtensionAsync("TestExtension");

        // Assert
        Assert.That(result, Is.True);

        // Verify removed
        var extensions = await _extensionManager.GetLoadedExtensionsAsync();
        Assert.That(extensions, Is.Empty);
    }

    [Test]
    public async Task UnregisterExtensionAsync_NonExistingExtension_ReturnsFalse()
    {
        // Act
        var result = await _extensionManager.UnregisterExtensionAsync("NonExistingExtension");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ReloadExtensionsAsync_ClearsAndReloads()
    {
        // Arrange
        await _extensionManager.RegisterExtensionAsync(_mockExtension.Object);

        // Act
        await _extensionManager.ReloadExtensionsAsync();

        // Assert
        // 現在の実装ではリロード時にクリアされるので、空になるはず
        var extensions = await _extensionManager.GetLoadedExtensionsAsync();
        Assert.That(extensions, Is.Empty);
    }

    // テスト用のツールクラス
    private class TestTool
    {
        // テスト用のツール実装
    }
}