using System.Net.Http;
using System.Net.Http.Json;

using System.Runtime.CompilerServices;

namespace Commanda.Core;

/// <summary>
/// OpenAI LLMプロバイダーの実装
/// </summary>
public class OpenAiProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly LlmProviderConfig _config;
    private readonly SecureStorage _secureStorage;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="config">プロバイダー設定</param>
    /// <param name="secureStorage">安全なストレージ</param>
    public OpenAiProvider(LlmProviderConfig config, SecureStorage secureStorage)
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
    public string ProviderType => "OpenAI";

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
        // APIキーを安全なストレージから取得
        var apiKey = await _secureStorage.RetrieveApiKeyAsync($"{_config.Name}_ApiKey");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException($"APIキーが設定されていません。プロバイダー: {_config.Name}");
        }

        // Authorizationヘッダーを設定
        _httpClient.DefaultRequestHeaders.Remove("Authorization");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var request = new
        {
            model = _config.ModelName ?? "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            max_tokens = 1000,
            temperature = 0.7
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_config.BaseUri ?? "https://api.openai.com/v1"}/chat/completions",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OpenAiResponse>(cancellationToken: cancellationToken);
        return result?.choices?[0]?.message?.content ?? "応答を取得できませんでした";
    }

    private class OpenAiResponse
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
