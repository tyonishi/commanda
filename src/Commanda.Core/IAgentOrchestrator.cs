namespace Commanda.Core;

/// <summary>
/// エージェント実行のオーケストレーションを担当するインターフェース
/// </summary>
public interface IAgentOrchestrator
{
    /// <summary>
    /// ユーザー入力を処理してタスクを実行します
    /// </summary>
    /// <param name="userInput">ユーザーの自然言語入力</param>
    /// <returns>実行結果</returns>
    Task<ExecutionResult> ExecuteTaskAsync(string userInput);

    /// <summary>
    /// 実行中のタスクをキャンセルします
    /// </summary>
    /// <returns>キャンセル処理のタスク</returns>
    Task CancelExecutionAsync();

    /// <summary>
    /// 現在の実行状態を取得します
    /// </summary>
    /// <returns>現在の実行状態</returns>
    ExecutionStatus GetCurrentStatus();
}

/// <summary>
/// 実行結果を表すクラス
/// </summary>
public class ExecutionResult
{
    /// <summary>
    /// 実行結果の内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 実行が成功したかどうか
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// エラーメッセージ
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// 警告メッセージのリスト
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// 実行にかかった時間
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// 実行されたステップ数
    /// </summary>
    public int StepsExecuted { get; set; }
}

/// <summary>
/// 実行状態を表す列挙型
/// </summary>
public enum ExecutionStatus
{
    /// <summary>
    /// 待機中
    /// </summary>
    Idle,

    /// <summary>
    /// 計画中
    /// </summary>
    Planning,

    /// <summary>
    /// 実行中
    /// </summary>
    Executing,

    /// <summary>
    /// 評価中
    /// </summary>
    Evaluating,

    /// <summary>
    /// 完了
    /// </summary>
    Completed,

    /// <summary>
    /// 失敗
    /// </summary>
    Failed,

    /// <summary>
    /// キャンセル
    /// </summary>
    Cancelled
}