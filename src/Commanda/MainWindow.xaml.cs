using System.Windows;
using System.Windows.Input;

namespace Commanda;

/// <summary>
/// MainWindow.xaml の相互作用ロジック
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        // キーボードショートカットの設定
        this.KeyDown += MainWindow_KeyDown;
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
}