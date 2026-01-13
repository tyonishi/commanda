namespace Commanda.Core;

/// <summary>
/// 実行計画を表すクラス
/// </summary>
public class ExecutionPlan
{
    /// <summary>
    /// 計画の概要説明
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 実行ステップのリスト
    /// </summary>
    public List<ExecutionStep> Steps { get; set; } = new();

    /// <summary>
    /// 追加のパラメータ
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}

/// <summary>
/// 実行ステップを表すクラス
/// </summary>
public class ExecutionStep
{
    /// <summary>
    /// ツール名
    /// </summary>
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    /// 引数
    /// </summary>
    public Dictionary<string, object> Arguments { get; set; } = new();

    /// <summary>
    /// 期待される結果
    /// </summary>
    public string ExpectedOutcome { get; set; } = string.Empty;

    /// <summary>
    /// タイムアウト時間
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// エージェントコンテキストを表すクラス
/// </summary>
public class AgentContext
{
    /// <summary>
    /// ユーザー入力
    /// </summary>
    public string UserInput { get; set; } = string.Empty;

    /// <summary>
    /// 現在の実行計画
    /// </summary>
    public ExecutionPlan? CurrentPlan { get; set; }

    /// <summary>
    /// 現在の実行状態
    /// </summary>
    public ExecutionStatus Status { get; set; }

    /// <summary>
    /// 実行履歴
    /// </summary>
    public List<ExecutionResult> ExecutionHistory { get; } = new();

    /// <summary>
    /// フィードバック履歴
    /// </summary>
    public List<string> FeedbackHistory { get; } = new();

    /// <summary>
    /// 完了しているかどうか
    /// </summary>
    public bool IsCompleted { get; private set; }

    /// <summary>
    /// キャンセルされているかどうか
    /// </summary>
    public bool IsCancelled { get; private set; }

    /// <summary>
    /// キャンセル理由
    /// </summary>
    public string? CancellationReason { get; private set; }

    /// <summary>
    /// 開始日時
    /// </summary>
    public DateTime StartedAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// 実行結果を追加します
    /// </summary>
    /// <param name="result">実行結果</param>
    public void AddExecutionResult(ExecutionResult result)
    {
        ExecutionHistory.Add(result);
    }

    /// <summary>
    /// フィードバックを追加します
    /// </summary>
    /// <param name="feedback">フィードバック</param>
    public void AddFeedback(string feedback)
    {
        FeedbackHistory.Add(feedback);
    }

    /// <summary>
    /// 完了としてマークします
    /// </summary>
    public void MarkCompleted()
    {
        IsCompleted = true;
        Status = ExecutionStatus.Completed;
    }

    /// <summary>
    /// キャンセルとしてマークします
    /// </summary>
    /// <param name="reason">キャンセル理由</param>
    public void MarkCancelled(string reason)
    {
        IsCancelled = true;
        CancellationReason = reason;
        Status = ExecutionStatus.Cancelled;
    }

    /// <summary>
    /// 最終レスポンスを取得します
    /// </summary>
    /// <returns>最終レスポンス</returns>
    public string GetFinalResponse()
    {
        if (IsCompleted)
        {
            var lastResult = ExecutionHistory.LastOrDefault();
            return lastResult?.Content ?? "タスクが完了しました。";
        }
        else
        {
            return $"タスクがキャンセルされました: {CancellationReason}";
        }
    }

    /// <summary>
    /// 警告を取得します
    /// </summary>
    /// <returns>警告のリスト</returns>
    public List<string> GetWarnings()
    {
        return ExecutionHistory
            .Where(r => !string.IsNullOrEmpty(r.Error))
            .Select(r => r.Error!)
            .ToList();
    }
}

/// <summary>
/// 実行ログを表すクラス
/// </summary>
public class ExecutionLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string TaskDescription { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Result { get; set; }
    public TimeSpan Duration { get; set; }
    public int StepsExecuted { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// タスク履歴を表すクラス
/// </summary>
public class TaskHistory
{
    public int Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string UserInput { get; set; } = string.Empty;
    public string ExecutionPlan { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? FinalResult { get; set; }
}

/// <summary>
/// 拡張機能情報を表すクラス
/// </summary>
public class ExtensionInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string AssemblyPath { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public DateTime InstalledAt { get; set; }
}
