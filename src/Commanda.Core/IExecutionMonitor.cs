namespace Commanda.Core;

/// <summary>
/// 実行モニターのインターフェース
/// </summary>
public interface IExecutionMonitor
{
    /// <summary>
    /// 実行結果を評価します
    /// </summary>
    /// <param name="result">実行結果</param>
    /// <param name="context">エージェントコンテキスト</param>
    /// <returns>評価結果</returns>
    Task<EvaluationResult> EvaluateResultAsync(ExecutionResult result, AgentContext context);
}

/// <summary>
/// 評価結果を表すクラス
/// </summary>
public class EvaluationResult
{
    /// <summary>
    /// 成功したかどうか
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// リトライが必要かどうか
    /// </summary>
    public bool ShouldRetry { get; set; }

    /// <summary>
    /// フィードバック
    /// </summary>
    public string Feedback { get; set; } = "";

    /// <summary>
    /// 理由
    /// </summary>
    public string Reason { get; set; } = "";
}

/// <summary>
/// 状態マネージャーのインターフェース
/// </summary>
public interface IStateManager
{
    /// <summary>
    /// 状態を保存します
    /// </summary>
    /// <param name="context">エージェントコンテキスト</param>
    /// <returns>保存処理のタスク</returns>
    Task SaveStateAsync(AgentContext context);

    /// <summary>
    /// 状態を読み込みます
    /// </summary>
    /// <param name="sessionId">セッションID</param>
    /// <returns>エージェントコンテキスト</returns>
    Task<AgentContext?> LoadStateAsync(string sessionId);

    /// <summary>
    /// 状態をクリアします
    /// </summary>
    /// <param name="sessionId">セッションID</param>
    /// <returns>クリア処理のタスク</returns>
    Task ClearStateAsync(string sessionId);
}
