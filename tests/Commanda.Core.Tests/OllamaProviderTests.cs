using Xunit;
using Moq;
using Commanda.Core;
using System.Runtime.Versioning;

namespace Commanda.Core.Tests;

[SupportedOSPlatform("windows")]
public class OllamaProviderTests
{
    private readonly Mock<SecureStorage> _secureStorageMock;
    private readonly LlmProviderConfig _config;

    public OllamaProviderTests()
    {
        _secureStorageMock = new Mock<SecureStorage>();
        _config = new LlmProviderConfig
        {
            Name = "Ollama",
            ProviderType = "Ollama",
            BaseUri = "http://localhost:11434",
            ModelName = "llama2"
        };
    }

    [Fact]
    public void Constructor_WithValidConfig_InitializesProperties()
    {
        // Arrange & Act
        var provider = new OllamaProvider(_config, _secureStorageMock.Object);

        // Assert
        Assert.Equal("Ollama", provider.Name);
        Assert.Equal("Ollama", provider.ProviderType);
    }

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new OllamaProvider(null!, _secureStorageMock.Object));
    }

    [Fact(Skip = "Requires Ollama server")]
    public async Task GetResponseAsync_WithValidPrompt_ReturnsResponse()
    {
        // Arrange
        var provider = new OllamaProvider(_config, _secureStorageMock.Object);

        // Act
        var response = await provider.GetResponseAsync("Hello!");

        // Assert
        Assert.NotNull(response);
    }

    [Fact(Skip = "Requires Ollama server")]
    public async Task StreamResponseAsync_WithValidPrompt_ReturnsChunks()
    {
        // Arrange
        var provider = new OllamaProvider(_config, _secureStorageMock.Object);
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

        var provider = new OllamaProvider(_config, _secureStorageMock.Object);

        // Act & Assert
        // TaskCanceledException is a subclass of OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await provider.GetResponseAsync("Hello", cancellationToken: cts.Token));
    }

    [Fact(Skip = "Requires Ollama server")]
    public async Task GetResponseAsync_WithCustomBaseUri_UsesCorrectEndpoint()
    {
        // Arrange
        var customConfig = new LlmProviderConfig
        {
            Name = "CustomOllama",
            ProviderType = "Ollama",
            BaseUri = "http://192.168.1.100:11434",
            ModelName = "mistral"
        };

        var provider = new OllamaProvider(customConfig, _secureStorageMock.Object);

        // Act
        var response = await provider.GetResponseAsync("Test");

        // Assert
        Assert.NotNull(response);
    }
}
