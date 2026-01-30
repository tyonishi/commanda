using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

namespace Commanda.Core;

/// <summary>
/// LM Studio LLMプロバイダーの実装
/// LM StudioはOpenAI互換のローカルLLMサーバーです
/// </summary>
public class LmStudioProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly LlmProviderConfig _config;
    private readonly SecureStorage _secureStorage;

    /// <summary>
    /// デフォルトのベースURI
    /// </summary>
    private const string DefaultBaseUri = "http://localhost:1234/v1";

    /// <summary>
    /// APIキーが不要な場合に使用するプレースホルダー値
    /// </summary>
    private const string NoApiKeyPlaceholder = "not-needed";

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="config">プロバイダー設定</param>
    /// <param name="secureStorage">安全なストレージ</param>
    public LmStudioProvider(LlmProviderConfig config, SecureStorage secureStorage)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _secureStorage = secureStorage ?? throw new ArgumentNullException(nameof(secureStorage));
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// プロバイダー名
    /// </summary>
    public string Name => _config.Name;

    /// <summary>
    /// プロバイダータイプ
    /// </summary>
    public string ProviderType => "LMStudio";

    /// <summary>
    /// ストリーミングレスポンスを取得します
    /// </summary>
    /// <param name="prompt">プロンプト</param>
    /// <param name="format">レスポンス形式</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>ストリーミングレスポンス</returns>
    public async IAsyncEnumerable<string> StreamResponseAsync(string prompt, ResponseFormat format = ResponseFormat.Text, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 簡易実装：同期レスポンスを1つのチャンクとして返す
        var response = await GetResponseAsync(prompt, format, cancellationToken);
        yield return response;
    }

    /// <summary>
    /// 同期レスポンスを取得します
    /// </summary>
    /// <param name="prompt">プロンプト</param>
    /// <param name="format">レスポンス形式</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>レスポンス</returns>
    public async Task<string> GetResponseAsync(string prompt, ResponseFormat format = ResponseFormat.Text, CancellationToken cancellationToken = default)
    {
        // APIキーを安全なストレージから取得（オプション）
        var apiKey = await _secureStorage.RetrieveApiKeyAsync($"{_config.Name}_ApiKey");

        // Authorizationヘッダーを設定（APIキーが設定されている場合のみ）
        _httpClient.DefaultRequestHeaders.Remove("Authorization");
        if (!string.IsNullOrEmpty(apiKey) && apiKey != NoApiKeyPlaceholder)
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }

        var request = new
        {
            model = _config.ModelName ?? "local-model",
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            max_tokens = 1000,
            temperature = 0.7
        };

        var baseUri = string.IsNullOrEmpty(_config.BaseUri) ? DefaultBaseUri : _config.BaseUri;

        var response = await _httpClient.PostAsJsonAsync(
            $"{baseUri}/chat/completions",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<LmStudioResponse>(cancellationToken: cancellationToken);
        return result?.choices?[0]?.message?.content ?? "応答を取得できませんでした";
    }

    /// <summary>
    /// LM Studio APIレスポンスモデル
    /// </summary>
    private class LmStudioResponse
    {
        public Choice[]? choices { get; set; }

        public class Choice
        {
            public Message? message { get; set; }
        }

        public class Message
        {
            public string? content { get; set; }
        }
    }
}
