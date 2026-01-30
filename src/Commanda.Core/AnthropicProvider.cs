using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Commanda.Core;

/// <summary>
/// Anthropic LLMプロバイダーの実装（Claude API）
/// </summary>
public class AnthropicProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly LlmProviderConfig _config;
    private readonly SecureStorage _secureStorage;
    private const string AnthropicVersion = "2023-06-01";

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="config">プロバイダー設定</param>
    /// <param name="secureStorage">安全なストレージ</param>
    public AnthropicProvider(LlmProviderConfig config, SecureStorage secureStorage)
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
    public string ProviderType => "Anthropic";

    /// <summary>
    /// ストリーミングレスポンスを取得します
    /// </summary>
    /// <param name="prompt">プロンプト</param>
    /// <param name="format">レスポンス形式</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>ストリーミングレスポンス</returns>
    public async IAsyncEnumerable<string> StreamResponseAsync(string prompt, ResponseFormat format = ResponseFormat.Text, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // APIキーを安全なストレージから取得
        var apiKey = await _secureStorage.RetrieveApiKeyAsync($"{_config.Name}_ApiKey");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException($"APIキーが設定されていません。プロバイダー: {_config.Name}");
        }

        // ヘッダーを設定
        SetAnthropicHeaders(apiKey);

        var request = CreateRequestBody(prompt, format, stream: true);
        var requestJson = JsonSerializer.Serialize(request);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync(
            $"{_config.BaseUri ?? "https://api.anthropic.com/v1"}/messages",
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
            {
                continue;
            }

            var data = line.Substring(6); // Remove "data: " prefix

            if (data == "[DONE]")
            {
                break;
            }

            var text = ParseStreamEvent(data);
            if (text != null)
            {
                yield return text;
            }
        }
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

        // ヘッダーを設定
        SetAnthropicHeaders(apiKey);

        var request = CreateRequestBody(prompt, format, stream: false);

        var response = await _httpClient.PostAsJsonAsync(
            $"{_config.BaseUri ?? "https://api.anthropic.com/v1"}/messages",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<AnthropicResponse>(cancellationToken: cancellationToken);
        
        if (result?.content == null || result.content.Length == 0)
        {
            return "応答を取得できませんでした";
        }

        // テキストコンテンツを結合
        var textContent = string.Join("", result.content
            .Where(c => c.type == "text")
            .Select(c => c.text));

        return textContent ?? "応答を取得できませんでした";
    }

    /// <summary>
    /// Anthropic API用のヘッダーを設定します
    /// </summary>
    /// <param name="apiKey">APIキー</param>
    private void SetAnthropicHeaders(string apiKey)
    {
        _httpClient.DefaultRequestHeaders.Remove("x-api-key");
        _httpClient.DefaultRequestHeaders.Remove("anthropic-version");
        _httpClient.DefaultRequestHeaders.Remove("Authorization");

        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", AnthropicVersion);
    }

    /// <summary>
    /// リクエストボディを作成します
    /// </summary>
    /// <param name="prompt">プロンプト</param>
    /// <param name="format">レスポンス形式</param>
    /// <param name="stream">ストリーミングするかどうか</param>
    /// <returns>リクエストボディオブジェクト</returns>
    private object CreateRequestBody(string prompt, ResponseFormat format, bool stream)
    {
        var messages = new[]
        {
            new { role = "user", content = prompt }
        };

        var request = new Dictionary<string, object>
        {
            ["model"] = _config.ModelName ?? "claude-3-sonnet-20240229",
            ["messages"] = messages,
            ["max_tokens"] = 1000,
            ["temperature"] = 0.7,
            ["stream"] = stream
        };

        // JSON形式の場合、システムプロンプトでJSON出力を指示
        if (format == ResponseFormat.JSON)
        {
            request["system"] = "You must respond with valid JSON only. Do not include any explanatory text outside the JSON structure.";
        }

        return request;
    }

    /// <summary>
    /// ストリーミングイベントをパースしてテキストを抽出します
    /// </summary>
    /// <param name="data">イベントデータ</param>
    /// <returns>抽出されたテキスト、無効な場合はnull</returns>
    private static string? ParseStreamEvent(string data)
    {
        try
        {
            var streamEvent = JsonSerializer.Deserialize<AnthropicStreamEvent>(data);
            if (streamEvent?.type == "content_block_delta" && streamEvent.delta?.text != null)
            {
                return streamEvent.delta.text;
            }
        }
        catch (JsonException)
        {
            // 無効なJSONは無視
        }

        return null;
    }

    /// <summary>
    /// Anthropic APIレスポンスモデル
    /// </summary>
    private class AnthropicResponse
    {
        public string? id { get; set; }
        public string? type { get; set; }
        public string? role { get; set; }
        public Content[]? content { get; set; }
        public string? model { get; set; }
        public string? stop_reason { get; set; }
        public string? stop_sequence { get; set; }
        public Usage? usage { get; set; }

        public class Content
        {
            public string? type { get; set; }
            public string? text { get; set; }
        }

        public class Usage
        {
            public int input_tokens { get; set; }
            public int output_tokens { get; set; }
        }
    }

    /// <summary>
    /// Anthropic APIストリーミングイベントモデル
    /// </summary>
    private class AnthropicStreamEvent
    {
        public string? type { get; set; }
        public int index { get; set; }
        public Delta? delta { get; set; }
        public Content? content_block { get; set; }

        public class Delta
        {
            public string? type { get; set; }
            public string? text { get; set; }
            public string? stop_reason { get; set; }
        }

        public class Content
        {
            public string? type { get; set; }
            public string? text { get; set; }
        }
    }
}
