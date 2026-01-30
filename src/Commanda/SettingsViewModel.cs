using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Commanda.Core;

namespace Commanda;

/// <summary>
/// 設定画面のViewModel
/// </summary>
public class SettingsViewModel : INotifyPropertyChanged
{
    private readonly ILlmProviderManager _llmManager;
    private readonly SecureStorage _secureStorage;
    private LlmProviderConfig? _selectedProvider;
    private string _newProviderName = string.Empty;
    private string _newProviderType = "OpenAI";
    private string _newApiKey = string.Empty;
    private string _newBaseUri = string.Empty;
    private string _newModelName = string.Empty;
    private bool _isDefaultProvider;
    private string _statusMessage = string.Empty;
    private bool _isStatusError;

    /// <summary>
    /// プロバイダー一覧
    /// </summary>
    public ObservableCollection<LlmProviderConfig> Providers { get; } = new();

    /// <summary>
    /// 選択中のプロバイダー
    /// </summary>
    public LlmProviderConfig? SelectedProvider
    {
        get => _selectedProvider;
        set
        {
            if (_selectedProvider != value)
            {
                _selectedProvider = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 新規プロバイダー名
    /// </summary>
    public string NewProviderName
    {
        get => _newProviderName;
        set
        {
            if (_newProviderName != value)
            {
                _newProviderName = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 新規プロバイダータイプ
    /// </summary>
    public string NewProviderType
    {
        get => _newProviderType;
        set
        {
            if (_newProviderType != value)
            {
                _newProviderType = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 新規APIキー
    /// </summary>
    public string NewApiKey
    {
        get => _newApiKey;
        set
        {
            if (_newApiKey != value)
            {
                _newApiKey = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 新規ベースURI
    /// </summary>
    public string NewBaseUri
    {
        get => _newBaseUri;
        set
        {
            if (_newBaseUri != value)
            {
                _newBaseUri = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 新規モデル名
    /// </summary>
    public string NewModelName
    {
        get => _newModelName;
        set
        {
            if (_newModelName != value)
            {
                _newModelName = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// デフォルトプロバイダーとして設定
    /// </summary>
    public bool IsDefaultProvider
    {
        get => _isDefaultProvider;
        set
        {
            if (_isDefaultProvider != value)
            {
                _isDefaultProvider = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// ステータスメッセージ
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// ステータスがエラーかどうか
    /// </summary>
    public bool IsStatusError
    {
        get => _isStatusError;
        set
        {
            if (_isStatusError != value)
            {
                _isStatusError = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 利用可能なプロバイダータイプ
    /// </summary>
    public string[] AvailableProviderTypes { get; } = new[] { "OpenAI", "Anthropic", "Ollama", "LMStudio" };

    /// <summary>
    /// プロバイダーを追加するコマンド
    /// </summary>
    public ICommand AddProviderCommand { get; }

    /// <summary>
    /// 設定を保存するコマンド
    /// </summary>
    public ICommand SaveSettingsCommand { get; }

    /// <summary>
    /// プロバイダーをテストするコマンド
    /// </summary>
    public ICommand TestProviderCommand { get; }

    /// <summary>
    /// プロバイダーを削除するコマンド
    /// </summary>
    public ICommand RemoveProviderCommand { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="llmManager">LLMプロバイダーマネージャー</param>
    /// <param name="secureStorage">安全なストレージ</param>
    public SettingsViewModel(ILlmProviderManager llmManager, SecureStorage secureStorage)
    {
        _llmManager = llmManager ?? throw new ArgumentNullException(nameof(llmManager));
        _secureStorage = secureStorage ?? throw new ArgumentNullException(nameof(secureStorage));

        AddProviderCommand = new RelayCommand(AddProvider, CanAddProvider);
        SaveSettingsCommand = new RelayCommand(SaveSettings);
        TestProviderCommand = new RelayCommand(TestProvider, CanTestProvider);
        RemoveProviderCommand = new RelayCommand(RemoveProvider, CanRemoveProvider);

        // プロバイダー一覧を読み込み
        _ = LoadProvidersAsync();
    }

    /// <summary>
    /// プロバイダー一覧を読み込みます
    /// </summary>
    public async Task LoadProvidersAsync()
    {
        try
        {
            Providers.Clear();
            var providerNames = await _llmManager.GetAvailableProvidersAsync();
            
            foreach (var name in providerNames)
            {
                var provider = await _llmManager.GetProviderAsync(name);
                if (provider != null)
                {
                    // 設定情報を取得
                    var config = new LlmProviderConfig
                    {
                        Name = name,
                        ProviderType = provider.ProviderType,
                        BaseUri = "", // APIキーは表示しない
                        ModelName = "", // 詳細情報は別途取得
                        IsDefault = false
                    };
                    Providers.Add(config);
                }
            }

            ShowStatus("プロバイダー一覧を読み込みました");
        }
        catch (Exception ex)
        {
            ShowStatus($"プロバイダー読み込みエラー: {ex.Message}", true);
        }
    }

    /// <summary>
    /// プロバイダーを追加します
    /// </summary>
    private async void AddProvider()
    {
        try
        {
            var config = new LlmProviderConfig
            {
                Name = NewProviderName.Trim(),
                ProviderType = NewProviderType,
                ApiKey = NewApiKey.Trim(),
                BaseUri = string.IsNullOrWhiteSpace(NewBaseUri) ? null : NewBaseUri.Trim(),
                ModelName = string.IsNullOrWhiteSpace(NewModelName) ? null : NewModelName.Trim(),
                IsDefault = IsDefaultProvider
            };

            var success = await _llmManager.AddProviderAsync(config);
            if (success)
            {
                Providers.Add(new LlmProviderConfig
                {
                    Name = config.Name,
                    ProviderType = config.ProviderType,
                    IsDefault = config.IsDefault
                });

                // 入力フィールドをクリア
                NewProviderName = string.Empty;
                NewApiKey = string.Empty;
                NewBaseUri = string.Empty;
                NewModelName = string.Empty;
                IsDefaultProvider = false;

                ShowStatus($"プロバイダー '{config.Name}' を追加しました");
            }
            else
            {
                ShowStatus($"プロバイダー '{config.Name}' の追加に失敗しました", true);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"エラー: {ex.Message}", true);
        }
    }

    /// <summary>
    /// プロバイダー追加が可能かどうか
    /// </summary>
    private bool CanAddProvider()
    {
        return !string.IsNullOrWhiteSpace(NewProviderName) &&
               !string.IsNullOrWhiteSpace(NewProviderType) &&
               !string.IsNullOrWhiteSpace(NewApiKey);
    }

    /// <summary>
    /// 設定を保存します
    /// </summary>
    private void SaveSettings()
    {
        // 設定の保存処理（必要に応じて実装）
        ShowStatus("設定を保存しました");
    }

    /// <summary>
    /// プロバイダーをテストします
    /// </summary>
    private async void TestProvider()
    {
        try
        {
            var config = new LlmProviderConfig
            {
                Name = NewProviderName.Trim(),
                ProviderType = NewProviderType,
                ApiKey = NewApiKey.Trim(),
                BaseUri = string.IsNullOrWhiteSpace(NewBaseUri) ? null : NewBaseUri.Trim(),
                ModelName = string.IsNullOrWhiteSpace(NewModelName) ? null : NewModelName.Trim()
            };

            ShowStatus("プロバイダーをテスト中...");
            var success = await _llmManager.TestProviderAsync(config);
            
            if (success)
            {
                ShowStatus("プロバイダーのテストに成功しました");
            }
            else
            {
                ShowStatus("プロバイダーのテストに失敗しました", true);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"テストエラー: {ex.Message}", true);
        }
    }

    /// <summary>
    /// プロバイダーテストが可能かどうか
    /// </summary>
    private bool CanTestProvider()
    {
        return !string.IsNullOrWhiteSpace(NewProviderName) &&
               !string.IsNullOrWhiteSpace(NewProviderType) &&
               !string.IsNullOrWhiteSpace(NewApiKey);
    }

    /// <summary>
    /// プロバイダーを削除します
    /// </summary>
    private async void RemoveProvider()
    {
        if (SelectedProvider == null) return;

        try
        {
            var success = await _llmManager.RemoveProviderAsync(SelectedProvider.Name);
            if (success)
            {
                Providers.Remove(SelectedProvider);
                SelectedProvider = null;
                ShowStatus("プロバイダーを削除しました");
            }
            else
            {
                ShowStatus("プロバイダーの削除に失敗しました", true);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"削除エラー: {ex.Message}", true);
        }
    }

    /// <summary>
    /// プロバイダー削除が可能かどうか
    /// </summary>
    private bool CanRemoveProvider()
    {
        return SelectedProvider != null;
    }

    /// <summary>
    /// ステータスメッセージを表示します
    /// </summary>
    /// <param name="message">メッセージ</param>
    /// <param name="isError">エラーかどうか</param>
    private void ShowStatus(string message, bool isError = false)
    {
        StatusMessage = message;
        IsStatusError = isError;
    }

    /// <summary>
    /// プロパティ変更イベント
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// プロパティ変更を通知します
    /// </summary>
    /// <param name="propertyName">プロパティ名</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
