using System.Collections.Concurrent;

namespace Commanda.Core;

/// <summary>
/// LLMプロバイダーマネージャーの実装
/// </summary>
public class LlmProviderManager : ILlmProviderManager
{
    private readonly ConcurrentDictionary<string, ILlmProvider> _providers = new();
    private string? _defaultProviderName;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public LlmProviderManager()
    {
        // デフォルトのOpenAIプロバイダーを設定
        var defaultConfig = new LlmProviderConfig
        {
            Name = "OpenAI",
            ProviderType = "OpenAI",
            BaseUri = "https://api.openai.com/v1",
            ModelName = "gpt-3.5-turbo",
            IsDefault = true
        };

        var provider = new OpenAiProvider(defaultConfig);
        _providers[defaultConfig.Name] = provider;
        _defaultProviderName = defaultConfig.Name;
    }

    /// <summary>
    /// アクティブなプロバイダーを取得します
    /// </summary>
    /// <returns>LLMプロバイダー</returns>
    public Task<ILlmProvider> GetActiveProviderAsync()
    {
        if (_defaultProviderName != null && _providers.TryGetValue(_defaultProviderName, out var provider))
        {
            return Task.FromResult(provider);
        }

        // 最初のプロバイダーを返す
        var firstProvider = _providers.Values.FirstOrDefault();
        return Task.FromResult(firstProvider ?? throw new InvalidOperationException("利用可能なプロバイダーがありません"));
    }

    /// <summary>
    /// 指定された名前のプロバイダーを取得します
    /// </summary>
    /// <param name="name">プロバイダー名</param>
    /// <returns>LLMプロバイダー</returns>
    public Task<ILlmProvider?> GetProviderAsync(string name)
    {
        _providers.TryGetValue(name, out var provider);
        return Task.FromResult(provider);
    }

    /// <summary>
    /// 利用可能なプロバイダーのリストを取得します
    /// </summary>
    /// <returns>プロバイダー名のリスト</returns>
    public Task<List<string>> GetAvailableProvidersAsync()
    {
        return Task.FromResult(_providers.Keys.ToList());
    }

    /// <summary>
    /// プロバイダーを追加します
    /// </summary>
    /// <param name="config">プロバイダー設定</param>
    /// <returns>追加が成功したかどうか</returns>
    public Task<bool> AddProviderAsync(LlmProviderConfig config)
    {
        if (_providers.ContainsKey(config.Name))
        {
            return Task.FromResult(false);
        }

        ILlmProvider provider = config.ProviderType switch
        {
            "OpenAI" => new OpenAiProvider(config),
            _ => throw new NotSupportedException($"プロバイダータイプ '{config.ProviderType}' はサポートされていません")
        };

        _providers[config.Name] = provider;

        if (config.IsDefault)
        {
            _defaultProviderName = config.Name;
        }

        return Task.FromResult(true);
    }

    /// <summary>
    /// プロバイダーを削除します
    /// </summary>
    /// <param name="name">プロバイダー名</param>
    /// <returns>削除が成功したかどうか</returns>
    public Task<bool> RemoveProviderAsync(string name)
    {
        var removed = _providers.TryRemove(name, out _);

        if (removed && _defaultProviderName == name)
        {
            _defaultProviderName = _providers.Keys.FirstOrDefault();
        }

        return Task.FromResult(removed);
    }

    /// <summary>
    /// プロバイダーをテストします
    /// </summary>
    /// <param name="config">プロバイダー設定</param>
    /// <returns>テスト結果</returns>
    public async Task<bool> TestProviderAsync(LlmProviderConfig config)
    {
        try
        {
            ILlmProvider? provider = null;

            if (config.ProviderType == "OpenAI")
            {
                provider = new OpenAiProvider(config);
            }
            else
            {
                return false;
            }

            var testPrompt = "Hello, please respond with 'OK' if you can understand this message.";
            var response = await provider.GetResponseAsync(testPrompt);

            return !string.IsNullOrEmpty(response);
        }
        catch
        {
            return false;
        }
    }
}