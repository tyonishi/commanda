using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Commanda.Core;

/// <summary>
/// Ollama local LLMプロバイダーの実装
/// </summary>
public class OllamaProvider : ILlmProvider
{
    private readonly HttpClient _httpClient;
    private readonly LlmProviderConfig _config;
    private readonly SecureStorage _secureStorage;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="config">プロバイダー設定</param>
    /// <param name="secureStorage">安全なストレージ</param>
    public OllamaProvider(LlmProviderConfig config, SecureStorage secureStorage)
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
    public string ProviderType => "Ollama";

    /// <summary>
    /// ストリーミングレスポンスを取得します
    /// </summary>
    /// <param name="prompt">プロンプト</param>
    /// <param name="format">レスポンス形式</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>ストリーミングレスポンス</returns>
    public async IAsyncEnumerable<string> StreamResponseAsync(string prompt, ResponseFormat format = ResponseFormat.Text, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = CreateRequestBody(prompt, format, stream: true);
        var requestJson = JsonSerializer.Serialize(request);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync(
            GetGenerateEndpoint(),
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var text = ParseStreamLine(line);
            if (text != null)
            {
                yield return text;
            }

            // doneフラグがtrueの場合は終了
            if (IsStreamComplete(line))
            {
                break;
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
        var request = CreateRequestBody(prompt, format, stream: false);

        var response = await _httpClient.PostAsJsonAsync(
            GetGenerateEndpoint(),
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken: cancellationToken);
        return result?.response ?? "応答を取得できませんでした";
    }

    /// <summary>
    /// generateエンドポイントのURLを取得します
    /// </summary>
    /// <returns>エンドポイントURL</returns>
    private string GetGenerateEndpoint()
    {
        var baseUri = _config.BaseUri ?? "http://localhost:11434";
        return $"{baseUri.TrimEnd('/')}/api/generate";
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
        var request = new Dictionary<string, object>
        {
            ["model"] = _config.ModelName ?? "llama2",
            ["prompt"] = prompt,
            ["stream"] = stream,
            ["options"] = new Dictionary<string, object>
            {
                ["temperature"] = 0.7
            }
        };

        // JSON形式の場合、formatパラメータを追加
        if (format == ResponseFormat.JSON)
        {
            request["format"] = "json";
        }

        return request;
    }

    /// <summary>
    /// ストリーミングレスポンスの行をパースしてテキストを抽出します
    /// </summary>
    /// <param name="line">NDJSON行</param>
    /// <returns>抽出されたテキスト、無効な場合はnull</returns>
    private static string? ParseStreamLine(string line)
    {
        try
        {
            var streamResponse = JsonSerializer.Deserialize<OllamaStreamResponse>(line);
            return streamResponse?.response;
        }
        catch (JsonException)
        {
            // 無効なJSONは無視
        }

        return null;
    }

    /// <summary>
    /// ストリーミングが完了したかどうかを判定します
    /// </summary>
    /// <param name="line">NDJSON行</param>
    /// <returns>完了した場合はtrue</returns>
    private static bool IsStreamComplete(string line)
    {
        try
        {
            var streamResponse = JsonSerializer.Deserialize<OllamaStreamResponse>(line);
            return streamResponse?.done ?? false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Ollama APIレスポンスモデル（非ストリーミング）
    /// </summary>
    private class OllamaResponse
    {
        public string? model { get; set; }
        public string? created_at { get; set; }
        public string? response { get; set; }
        public bool done { get; set; }
        public long[]? context { get; set; }
        public long? total_duration { get; set; }
        public long? load_duration { get; set; }
        public int? prompt_eval_count { get; set; }
        public long? prompt_eval_duration { get; set; }
        public int? eval_count { get; set; }
        public long? eval_duration { get; set; }
    }

    /// <summary>
    /// Ollama APIストリーミングレスポンスモデル
    /// </summary>
    private class OllamaStreamResponse
    {
        public string? model { get; set; }
        public string? created_at { get; set; }
        public string? response { get; set; }
        public bool done { get; set; }
    }
}
