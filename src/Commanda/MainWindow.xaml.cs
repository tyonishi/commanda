using System.Windows;
using System.Windows.Input;
using Commanda.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Commanda;

/// <summary>
/// MainWindow.xaml の相互作用ロジック
/// </summary>
public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;

    public MainWindow()
    {
        InitializeComponent();
        // キーボードショートカットの設定
        this.KeyDown += MainWindow_KeyDown;
        
        // DIコンテナを取得
        _serviceProvider = ((App)Application.Current).ServiceProvider;
    }

    /// <summary>
    /// キーボードショートカット処理
    /// </summary>
    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        // Ctrl+Enter: コマンド送信
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (DataContext is MainViewModel viewModel && viewModel.SendCommand.CanExecute(null))
            {
                viewModel.SendCommand.Execute(null);
                e.Handled = true;
            }
        }
        // Escape: 実行中の場合はキャンセル
        else if (e.Key == Key.Escape)
        {
            if (DataContext is MainViewModel viewModel && viewModel.IsExecuting)
            {
                // キャンセルコマンドがあれば実行
                if (viewModel.CancelCommand?.CanExecute(null) == true)
                {
                    viewModel.CancelCommand.Execute(null);
                    e.Handled = true;
                }
            }
        }
        // F1: ヘルプ表示（将来の実装）
        else if (e.Key == Key.F1)
        {
            ShowHelp();
            e.Handled = true;
        }
    }

    /// <summary>
    /// ヘルプを表示
    /// </summary>
    private void ShowHelp()
    {
        MessageBox.Show(
            "Commanda - AI PC Automation Tool\n\n" +
            "使い方:\n" +
            "- テキストボックスに自然言語で指示を入力\n" +
            "- EnterまたはCtrl+Enterで送信\n" +
            "- Escapeで実行をキャンセル\n" +
            "- F1でこのヘルプを表示\n\n" +
            "例:\n" +
            "- 「test.txtファイルを作成してこんにちはと書き込んで」\n" +
            "- 「デスクトップのファイルを整理して」",
            "Commanda ヘルプ",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    /// <summary>
    /// 設定メニュー項目クリック
    /// </summary>
    private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var llmManager = _serviceProvider.GetRequiredService<ILlmProviderManager>();
            var secureStorage = _serviceProvider.GetRequiredService<SecureStorage>();
            
            var settingsWindow = new SettingsWindow(llmManager, secureStorage);
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"設定画面を開けません: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 実行履歴メニュー項目クリック
    /// </summary>
    private void HistoryMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<ExecutionLog>>();
            
            var historyWindow = new HistoryWindow(repository);
            historyWindow.Owner = this;
            historyWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"実行履歴画面を開けません: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// 終了メニュー項目クリック
    /// </summary>
    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// バージョン情報メニュー項目クリック
    /// </summary>
    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Commanda v1.0.0\n\n" +
            "AI-powered PC automation tool\n" +
            "Built with .NET 8.0 and WPF\n\n" +
            "© 2026 Commanda Development Team",
            "バージョン情報",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}