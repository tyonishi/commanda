using Xunit;
using Moq;
using Commanda.Core;
using System.ComponentModel;
using System.Runtime.Versioning;

namespace Commanda.UI.Tests;

[SupportedOSPlatform("windows")]
public class SettingsViewModelTests
{
    private readonly Mock<ILlmProviderManager> _llmManagerMock;
    private readonly Mock<SecureStorage> _secureStorageMock;
    private readonly SettingsViewModel _viewModel;

    public SettingsViewModelTests()
    {
        _llmManagerMock = new Mock<ILlmProviderManager>();
        _secureStorageMock = new Mock<SecureStorage>();
        _viewModel = new SettingsViewModel(_llmManagerMock.Object, _secureStorageMock.Object);
    }

    [Fact]
    public void Constructor_InitializesProvidersCollection()
    {
        // Assert
        Assert.NotNull(_viewModel.Providers);
        Assert.Empty(_viewModel.Providers);
    }

    [Fact]
    public void Constructor_InitializesCommands()
    {
        // Assert
        Assert.NotNull(_viewModel.AddProviderCommand);
        Assert.NotNull(_viewModel.SaveSettingsCommand);
        Assert.NotNull(_viewModel.TestProviderCommand);
        Assert.NotNull(_viewModel.RemoveProviderCommand);
    }

    [Fact]
    public void SelectedProvider_SetValue_RaisesPropertyChanged()
    {
        // Arrange
        var propertyChangedRaised = false;
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.SelectedProvider))
                propertyChangedRaised = true;
        };

        var provider = new LlmProviderConfig { Name = "Test" };

        // Act
        _viewModel.SelectedProvider = provider;

        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal(provider, _viewModel.SelectedProvider);
    }

    [Fact]
    public void AddProviderCommand_WithValidData_AddsProvider()
    {
        // Arrange
        _viewModel.NewProviderName = "TestProvider";
        _viewModel.NewProviderType = "OpenAI";
        _viewModel.NewApiKey = "sk-test";
        _viewModel.NewBaseUri = "https://api.test.com";
        _viewModel.NewModelName = "gpt-4";

        _llmManagerMock.Setup(m => m.AddProviderAsync(It.IsAny<LlmProviderConfig>()))
            .ReturnsAsync(true);

        // Act
        _viewModel.AddProviderCommand.Execute(null);

        // Assert
        _llmManagerMock.Verify(m => m.AddProviderAsync(It.Is<LlmProviderConfig>(
            p => p.Name == "TestProvider" &&
                 p.ProviderType == "OpenAI" &&
                 p.BaseUri == "https://api.test.com" &&
                 p.ModelName == "gpt-4")), Times.Once);
    }
}
