using System.Windows;
using Commanda.Core;
using Commanda.Extensions;
using Commanda.Mcp;

namespace Commanda;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 簡易的な依存性注入
        var extensionManager = new ExtensionManager();
        var mcpServer = new McpServer(extensionManager);
        var llmManager = new LlmProviderManager();
        var agentOrchestrator = new AgentOrchestrator(llmManager, mcpServer);

        // MainWindowの作成と表示
        var mainWindow = new MainWindow();
        var viewModel = new MainViewModel(agentOrchestrator);
        mainWindow.DataContext = viewModel;
        mainWindow.Show();
    }
}