namespace Commanda.Core;

/// <summary>
/// LLMプロバイダーマネージャーのインターフェース
/// </summary>
public interface ILlmProviderManager
{
    /// <summary>
    /// アクティブなプロバイダーを取得します
    /// </summary>
    /// <returns>LLMプロバイダー</returns>
    Task<ILlmProvider> GetActiveProviderAsync();

    /// <summary>
    /// 指定された名前のプロバイダーを取得します
    /// </summary>
    /// <param name="name">プロバイダー名</param>
    /// <returns>LLMプロバイダー</returns>
    Task<ILlmProvider?> GetProviderAsync(string name);

    /// <summary>
    /// 利用可能なプロバイダーのリストを取得します
    /// </summary>
    /// <returns>プロバイダー名のリスト</returns>
    Task<List<string>> GetAvailableProvidersAsync();

    /// <summary>
    /// プロバイダーを追加します
    /// </summary>
    /// <param name="config">プロバイダー設定</param>
    /// <returns>追加が成功したかどうか</returns>
    Task<bool> AddProviderAsync(LlmProviderConfig config);

    /// <summary>
    /// プロバイダーを削除します
    /// </summary>
    /// <param name="name">プロバイダー名</param>
    /// <returns>削除が成功したかどうか</returns>
    Task<bool> RemoveProviderAsync(string name);

    /// <summary>
    /// プロバイダーをテストします
    /// </summary>
    /// <param name="config">プロバイダー設定</param>
    /// <returns>テスト結果</returns>
    Task<bool> TestProviderAsync(LlmProviderConfig config);
}

/// <summary>
/// LLMプロバイダーのインターフェース
/// </summary>
public interface ILlmProvider
{
    /// <summary>
    /// プロバイダー名
    /// </summary>
    string Name { get; }

    /// <summary>
    /// プロバイダータイプ
    /// </summary>
    string ProviderType { get; }

    /// <summary>
    /// ストリーミングレスポンスを取得します
    /// </summary>
    /// <param name="prompt">プロンプト</param>
    /// <param name="format">レスポンス形式</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>ストリーミングレスポンス</returns>
    IAsyncEnumerable<string> StreamResponseAsync(string prompt, ResponseFormat format = ResponseFormat.Text, CancellationToken cancellationToken = default);

    /// <summary>
    /// 同期レスポンスを取得します
    /// </summary>
    /// <param name="prompt">プロンプト</param>
    /// <param name="format">レスポンス形式</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>レスポンス</returns>
    Task<string> GetResponseAsync(string prompt, ResponseFormat format = ResponseFormat.Text, CancellationToken cancellationToken = default);
}

/// <summary>
/// レスポンス形式を表す列挙型
/// </summary>
public enum ResponseFormat
{
    /// <summary>
    /// テキスト形式
    /// </summary>
    Text,

    /// <summary>
    /// JSON形式
    /// </summary>
    JSON
}

/// <summary>
/// LLMプロバイダー設定を表すクラス
/// </summary>
public class LlmProviderConfig
{
    /// <summary>
    /// ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// プロバイダー名
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// プロバイダータイプ
    /// </summary>
    public string ProviderType { get; set; } = string.Empty;

    /// <summary>
    /// APIキー（暗号化済み）
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// ベースURI
    /// </summary>
    public string? BaseUri { get; set; }

    /// <summary>
    /// モデル名
    /// </summary>
    public string? ModelName { get; set; }

    /// <summary>
    /// デフォルトプロバイダーかどうか
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 作成日時
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最終検証日時
    /// </summary>
    public DateTime? LastValidated { get; set; }
}
