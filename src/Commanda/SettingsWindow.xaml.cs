using System.Windows;
using Commanda.Core;

namespace Commanda;

/// <summary>
/// 設定画面
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow(ILlmProviderManager llmManager, SecureStorage secureStorage)
    {
        InitializeComponent();
        DataContext = new SettingsViewModel(llmManager, secureStorage);
    }

    /// <summary>
    /// APIキーのPasswordBoxからViewModelに値を渡す
    /// </summary>
    private void ApiKeyPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.NewApiKey = ApiKeyPasswordBox.Password;
        }
    }

    /// <summary>
    /// 保存して閉じるボタンのクリックハンドラ
    /// </summary>
    private void SaveAndCloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel viewModel)
        {
            viewModel.SaveSettingsCommand.Execute(null);
        }
        Close();
    }
}
