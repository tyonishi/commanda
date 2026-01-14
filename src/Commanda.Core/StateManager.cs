using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Commanda.Core;

/// <summary>
/// 状態マネージャーの実装
/// </summary>
public class StateManager : IStateManager
{
    private readonly ILogger<StateManager> _logger;
    private readonly string _stateDirectory;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="logger">ロガー</param>
    /// <param name="stateDirectory">状態ファイル保存ディレクトリ（オプション）</param>
    public StateManager(ILogger<StateManager> logger, string? stateDirectory = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stateDirectory = stateDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Commanda",
            "State");

        // ディレクトリが存在しない場合は作成
        if (!Directory.Exists(_stateDirectory))
        {
            Directory.CreateDirectory(_stateDirectory);
        }
    }

    /// <summary>
    /// 状態を保存します
    /// </summary>
    /// <param name="context">エージェントコンテキスト</param>
    /// <returns>保存処理のタスク</returns>
    public async Task SaveStateAsync(AgentContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var sessionId = GenerateSessionId(context);
        var filePath = GetStateFilePath(sessionId);

        try
        {
            // シリアライズ可能な状態オブジェクトを作成
            var state = new StateSnapshot
            {
                SessionId = sessionId,
                UserInput = context.UserInput,
                Status = context.Status,
                CurrentPlan = context.CurrentPlan,
                ExecutionHistory = context.ExecutionHistory,
                FeedbackHistory = context.FeedbackHistory,
                IsCompleted = context.IsCompleted,
                IsCancelled = context.IsCancelled,
                CancellationReason = context.CancellationReason,
                StartedAt = context.StartedAt,
                LastUpdatedAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            // テンポラリファイルに書き込んでから移動（原子性確保）
            var tempPath = filePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json, System.Text.Encoding.UTF8);

            if (File.Exists(filePath))
            {
                File.Replace(tempPath, filePath, null);
            }
            else
            {
                File.Move(tempPath, filePath);
            }

            _logger.LogInformation("エージェント状態を保存しました: {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "状態の保存に失敗しました: {SessionId}", sessionId);
            throw new StateManagerException($"状態の保存に失敗しました: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 状態を読み込みます
    /// </summary>
    /// <param name="sessionId">セッションID</param>
    /// <returns>エージェントコンテキスト</returns>
    public async Task<AgentContext?> LoadStateAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("セッションIDは空にできません", nameof(sessionId));
        }

        var filePath = GetStateFilePath(sessionId);

        if (!File.Exists(filePath))
        {
            _logger.LogInformation("状態ファイルが見つかりません: {SessionId}", sessionId);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, System.Text.Encoding.UTF8);
            var state = JsonSerializer.Deserialize<StateSnapshot>(json);

            if (state == null)
            {
                _logger.LogWarning("状態データのデシリアライズに失敗しました: {SessionId}", sessionId);
                return null;
            }

            // AgentContextに変換
            var context = new AgentContext
            {
                UserInput = state.UserInput,
                Status = state.Status,
                CurrentPlan = state.CurrentPlan,
                IsCompleted = state.IsCompleted,
                IsCancelled = state.IsCancelled,
                CancellationReason = state.CancellationReason,
                StartedAt = state.StartedAt
            };

            // 履歴の復元
            foreach (var result in state.ExecutionHistory)
            {
                context.AddExecutionResult(result);
            }

            foreach (var feedback in state.FeedbackHistory)
            {
                context.AddFeedback(feedback);
            }

            _logger.LogInformation("エージェント状態を読み込みました: {SessionId}", sessionId);
            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "状態の読み込みに失敗しました: {SessionId}", sessionId);
            throw new StateManagerException($"状態の読み込みに失敗しました: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 状態をクリアします
    /// </summary>
    /// <param name="sessionId">セッションID</param>
    /// <returns>クリア処理のタスク</returns>
    public Task ClearStateAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            throw new ArgumentException("セッションIDは空にできません", nameof(sessionId));
        }

        var filePath = GetStateFilePath(sessionId);

        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("エージェント状態をクリアしました: {SessionId}", sessionId);
            }
            else
            {
                _logger.LogInformation("クリア対象の状態ファイルが見つかりません: {SessionId}", sessionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "状態のクリアに失敗しました: {SessionId}", sessionId);
            throw new StateManagerException($"状態のクリアに失敗しました: {ex.Message}", ex);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// セッションIDを生成します
    /// </summary>
    /// <param name="context">エージェントコンテキスト</param>
    /// <returns>セッションID</returns>
    private string GenerateSessionId(AgentContext context)
    {
        // ユーザー入力と開始時間に基づいてセッションIDを生成
        var input = context.UserInput ?? "unknown";
        var time = context.StartedAt.ToString("yyyyMMddHHmmss");
        var hash = input.GetHashCode().ToString("X8");
        return $"{time}_{hash}";
    }

    /// <summary>
    /// 状態ファイルのパスを取得します
    /// </summary>
    /// <param name="sessionId">セッションID</param>
    /// <returns>ファイルパス</returns>
    private string GetStateFilePath(string sessionId)
    {
        return Path.Combine(_stateDirectory, $"{sessionId}.state.json");
    }

    /// <summary>
    /// 古い状態ファイルをクリーンアップします
    /// </summary>
    /// <param name="daysOld">何日以上前のファイルを削除するか</param>
    /// <returns>クリーンアップ処理のタスク</returns>
    public async Task CleanupOldStatesAsync(int daysOld = 7)
    {
        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
            var stateFiles = Directory.GetFiles(_stateDirectory, "*.state.json");

            foreach (var filePath in stateFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.LastWriteTimeUtc < cutoffDate)
                    {
                        await Task.Run(() => File.Delete(filePath));
                        _logger.LogInformation("古い状態ファイルを削除しました: {FileName}", Path.GetFileName(filePath));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "古い状態ファイルの削除に失敗しました: {FileName}", Path.GetFileName(filePath));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "古い状態ファイルのクリーンアップに失敗しました");
            throw new StateManagerException($"古い状態ファイルのクリーンアップに失敗しました: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// 状態スナップショットを表すクラス
/// </summary>
public class StateSnapshot
{
    /// <summary>
    /// セッションID
    /// </summary>
    public string SessionId { get; set; } = "";

    /// <summary>
    /// ユーザー入力
    /// </summary>
    public string UserInput { get; set; } = "";

    /// <summary>
    /// 現在の実行状態
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ExecutionStatus Status { get; set; }

    /// <summary>
    /// 現在の実行計画
    /// </summary>
    public ExecutionPlan? CurrentPlan { get; set; }

    /// <summary>
    /// 実行履歴
    /// </summary>
    public List<ExecutionResult> ExecutionHistory { get; set; } = new();

    /// <summary>
    /// フィードバック履歴
    /// </summary>
    public List<string> FeedbackHistory { get; set; } = new();

    /// <summary>
    /// 完了しているかどうか
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// キャンセルされているかどうか
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// キャンセル理由
    /// </summary>
    public string? CancellationReason { get; set; }

    /// <summary>
    /// 開始日時
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// 最終更新日時
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }
}

/// <summary>
/// 状態マネージャーの例外クラス
/// </summary>
public class StateManagerException : Exception
{
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="message">メッセージ</param>
    public StateManagerException(string message) : base(message)
    {
    }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="message">メッセージ</param>
    /// <param name="innerException">内部例外</param>
    public StateManagerException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
