using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Commanda.Core;

/// <summary>
/// 設定管理マネージャーの実装
/// </summary>
public class SettingsManager : ISettingsManager
{
    private readonly string _settingsFilePath;
    private readonly ILogger<SettingsManager> _logger;
    private readonly SecureStorage _secureStorage;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="logger">ロガー</param>
    public SettingsManager(ILogger<SettingsManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _secureStorage = new SecureStorage();
        
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Commanda");
        
        if (!Directory.Exists(settingsDir))
        {
            Directory.CreateDirectory(settingsDir);
        }
        
        _settingsFilePath = Path.Combine(settingsDir, "settings.json");
    }

    /// <summary>
    /// LLM設定を読み込みます
    /// </summary>
    /// <returns>LLM設定</returns>
    public async Task<LlmSettings> LoadLlmSettingsAsync()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                _logger.LogInformation("設定ファイルが存在しません。新規作成します。");
                return new LlmSettings();
            }

            var json = await File.ReadAllTextAsync(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<LlmSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return settings ?? new LlmSettings();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "設定の読み込みに失敗しました");
            return new LlmSettings();
        }
    }

    /// <summary>
    /// LLM設定を保存します
    /// </summary>
    /// <param name="settings">LLM設定</param>
    public async Task SaveLlmSettingsAsync(LlmSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await File.WriteAllTextAsync(_settingsFilePath, json);
            _logger.LogInformation("設定を保存しました");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "設定の保存に失敗しました");
            throw;
        }
    }

    /// <summary>
    /// LLMプロバイダーを追加します
    /// </summary>
    /// <param name="config">プロバイダー設定</param>
    /// <returns>追加されたプロバイダー設定</returns>
    public async Task<LlmProviderConfig> AddProviderAsync(LlmProviderConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        if (string.IsNullOrWhiteSpace(config.Name))
        {
            throw new ArgumentException("プロバイダー名は必須です", nameof(config));
        }

        var settings = await LoadLlmSettingsAsync();
        
        // 既存のプロバイダーをチェック
        var existingProvider = settings.Providers.FirstOrDefault(p => p.Name == config.Name);
        if (existingProvider != null)
        {
            // 既存のプロバイダーを更新
            existingProvider.ProviderType = config.ProviderType;
            existingProvider.ApiKey = config.ApiKey;
            existingProvider.BaseUri = config.BaseUri;
            existingProvider.ModelName = config.ModelName;
            existingProvider.IsDefault = config.IsDefault;
            existingProvider.LastValidated = DateTime.UtcNow;
            
            _logger.LogInformation("プロバイダー '{ProviderName}' を更新しました", config.Name);
        }
        else
        {
            // 新規プロバイダーを追加
            config.Id = settings.Providers.Count > 0 ? settings.Providers.Max(p => p.Id) + 1 : 1;
            config.CreatedAt = DateTime.UtcNow;
            config.LastValidated = DateTime.UtcNow;
            
            settings.Providers.Add(config);
            _logger.LogInformation("プロバイダー '{ProviderName}' を追加しました", config.Name);
        }

        // デフォルトプロバイダーの設定
        if (config.IsDefault)
        {
            settings.DefaultProviderName = config.Name;
            // 他のプロバイダーのデフォルトフラグを解除
            foreach (var provider in settings.Providers.Where(p => p.Name != config.Name))
            {
                provider.IsDefault = false;
            }
        }

        await SaveLlmSettingsAsync(settings);
        return config;
    }

    /// <summary>
    /// LLMプロバイダーを削除します
    /// </summary>
    /// <param name="name">プロバイダー名</param>
    /// <returns>削除が成功したかどうか</returns>
    public async Task<bool> RemoveProviderAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("プロバイダー名は必須です", nameof(name));
        }

        var settings = await LoadLlmSettingsAsync();
        var provider = settings.Providers.FirstOrDefault(p => p.Name == name);
        
        if (provider == null)
        {
            _logger.LogWarning("削除対象のプロバイダー '{ProviderName}' が見つかりません", name);
            return false;
        }

        settings.Providers.Remove(provider);
        
        // デフォルトプロバイダーが削除された場合、デフォルトをリセット
        if (settings.DefaultProviderName == name)
        {
            settings.DefaultProviderName = null;
        }

        await SaveLlmSettingsAsync(settings);
        _logger.LogInformation("プロバイダー '{ProviderName}' を削除しました", name);
        return true;
    }
}
