namespace Commanda.Core;

/// <summary>
/// 設定管理マネージャーのインターフェース
/// </summary>
public interface ISettingsManager
{
    /// <summary>
    /// LLM設定を読み込みます
    /// </summary>
    /// <returns>LLM設定</returns>
    Task<LlmSettings> LoadLlmSettingsAsync();

    /// <summary>
    /// LLM設定を保存します
    /// </summary>
    /// <param name="settings">LLM設定</param>
    /// <returns>保存処理のタスク</returns>
    Task SaveLlmSettingsAsync(LlmSettings settings);

    /// <summary>
    /// LLMプロバイダーを追加します
    /// </summary>
    /// <param name="config">プロバイダー設定</param>
    /// <returns>追加されたプロバイダー設定</returns>
    Task<LlmProviderConfig> AddProviderAsync(LlmProviderConfig config);

    /// <summary>
    /// LLMプロバイダーを削除します
    /// </summary>
    /// <param name="name">プロバイダー名</param>
    /// <returns>削除が成功したかどうか</returns>
    Task<bool> RemoveProviderAsync(string name);
}

/// <summary>
/// LLM設定を表すクラス
/// </summary>
public class LlmSettings
{
    /// <summary>
    /// プロバイダー設定のリスト
    /// </summary>
    public List<LlmProviderConfig> Providers { get; set; } = new();

    /// <summary>
    /// デフォルトプロバイダー名
    /// </summary>
    public string? DefaultProviderName { get; set; }

    /// <summary>
    /// 指定された名前のプロバイダーを取得します
    /// </summary>
    /// <param name="name">プロバイダー名</param>
    /// <returns>プロバイダー設定</returns>
    public LlmProviderConfig? GetProvider(string name)
    {
        return Providers.FirstOrDefault(p => p.Name == name);
    }

    /// <summary>
    /// デフォルトプロバイダーを取得します
    /// </summary>
    /// <returns>デフォルトプロバイダー設定</returns>
    public LlmProviderConfig? GetDefaultProvider()
    {
        if (!string.IsNullOrEmpty(DefaultProviderName))
        {
            return GetProvider(DefaultProviderName);
        }
        return Providers.FirstOrDefault(p => p.IsDefault);
    }
}
