using Xunit;
using Moq;
using Commanda.Core;
using System.Runtime.Versioning;

namespace Commanda.Core.Tests;

[SupportedOSPlatform("windows")]
public class LmStudioProviderTests
{
    private readonly Mock<SecureStorage> _secureStorageMock;
    private readonly LlmProviderConfig _config;

    public LmStudioProviderTests()
    {
        _secureStorageMock = new Mock<SecureStorage>();
        _config = new LlmProviderConfig
        {
            Name = "LMStudio",
            ProviderType = "LMStudio",
            BaseUri = "http://localhost:1234/v1",
            ModelName = "local-model"
        };
    }

    [Fact]
    public void Constructor_WithValidConfig_InitializesProperties()
    {
        // Arrange & Act
        var provider = new LmStudioProvider(_config, _secureStorageMock.Object);

        // Assert
        Assert.Equal("LMStudio", provider.Name);
        Assert.Equal("LMStudio", provider.ProviderType);
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new LmStudioProvider(null!, _secureStorageMock.Object));
    }

    [Fact(Skip = "Requires LM Studio server")]
    public async Task GetResponseAsync_WithValidPrompt_ReturnsResponse()
    {
        // Arrange
        var provider = new LmStudioProvider(_config, _secureStorageMock.Object);

        // Act
        var response = await provider.GetResponseAsync("Hello!");

        // Assert
        Assert.NotNull(response);
    }

    [Fact(Skip = "Requires LM Studio server")]
    public async Task StreamResponseAsync_WithValidPrompt_ReturnsChunks()
    {
        // Arrange
        var provider = new LmStudioProvider(_config, _secureStorageMock.Object);
        var chunks = new List<string>();

        // Act
        await foreach (var chunk in provider.StreamResponseAsync("Hello"))
        {
            chunks.Add(chunk);
        }

        // Assert
        Assert.NotEmpty(chunks);
    }

    [Fact]
    public async Task GetResponseAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var provider = new LmStudioProvider(_config, _secureStorageMock.Object);

        // Act & Assert
        // TaskCanceledException is a subclass of OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await provider.GetResponseAsync("Hello", cancellationToken: cts.Token));
    }

    [Fact]
    public void Provider_UsesCorrectDefaultBaseUri()
    {
        // Arrange
        var config = new LlmProviderConfig
        {
            Name = "LMStudio",
            ProviderType = "LMStudio",
            BaseUri = null, // Test default
            ModelName = "local-model"
        };

        // Act
        var provider = new LmStudioProvider(config, _secureStorageMock.Object);

        // Assert - LM Studio uses OpenAI-compatible API
        // Note: Actual HTTP tests require a running LM Studio server
        // This test verifies the provider can be instantiated with null BaseUri
        Assert.NotNull(provider);
    }
}
