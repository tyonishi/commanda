using System.Windows;
using Commanda.Core;
using Commanda.Extensions;
using Commanda.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Commanda;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Core services
                services.AddSingleton<ILlmProviderManager, LlmProviderManager>();
                services.AddSingleton<IMcpServer, McpServer>();
                services.AddSingleton<IAgentOrchestrator, AgentOrchestrator>();

                // Extensions
                services.AddSingleton<IExtensionManager, ExtensionManager>();

                // WPF services
                services.AddTransient<MainViewModel>();
            })
            .Build();

        await _host.StartAsync();

        // MainWindowの作成と表示
        var mainWindow = new MainWindow();
        var mainViewModel = _host.Services.GetRequiredService<MainViewModel>();
        mainWindow.DataContext = mainViewModel;
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}