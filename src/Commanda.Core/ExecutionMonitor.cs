using Microsoft.Extensions.Logging;

namespace Commanda.Core;

/// <summary>
/// 実行モニターの実装
/// </summary>
public class ExecutionMonitor : IExecutionMonitor
{
    private readonly ILogger<ExecutionMonitor> _logger;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="logger">ロガー</param>
    public ExecutionMonitor(ILogger<ExecutionMonitor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 実行結果を評価します
    /// </summary>
    /// <param name="result">実行結果</param>
    /// <param name="context">エージェントコンテキスト</param>
    /// <returns>評価結果</returns>
    public Task<EvaluationResult> EvaluateResultAsync(ExecutionResult result, AgentContext context)
    {
        _logger.LogInformation("実行結果を評価しています: {Status}, 実行時間: {Duration}ms",
            result.IsSuccessful ? "成功" : "失敗", result.Duration.TotalMilliseconds);

        var evaluation = new EvaluationResult();

        // 基本的な評価ロジック
        if (result.IsSuccessful)
        {
            evaluation.IsSuccessful = true;
            evaluation.Reason = "実行が正常に完了しました";

            // 実行時間が長すぎる場合の警告
            if (result.Duration > TimeSpan.FromMinutes(5))
            {
                evaluation.Feedback = "実行時間が長くなっています。最適化を検討してください。";
                _logger.LogWarning("実行時間が長いタスクを検出: {Duration}", result.Duration);
            }
        }
        else
        {
            evaluation.IsSuccessful = false;
            evaluation.Reason = "実行に失敗しました";

            // エラー内容に基づいてリトライ判断
            if (IsRetryableError(result))
            {
                evaluation.ShouldRetry = true;
                evaluation.Feedback = "一時的なエラーの可能性があります。リトライを推奨します。";
                _logger.LogWarning("リトライ可能なエラーを検出: {Error}", result.Error);
            }
            else
            {
                evaluation.ShouldRetry = false;
                evaluation.Feedback = "恒久的なエラーのため、リトライを中止します。";
                _logger.LogError("恒久的なエラーを検出: {Error}", result.Error);
            }
        }

        // 実行履歴に基づく追加評価
        EvaluateExecutionHistory(context, evaluation);

        return Task.FromResult(evaluation);
    }

    /// <summary>
    /// リトライ可能なエラーかどうかを判断します
    /// </summary>
    /// <param name="result">実行結果</param>
    /// <returns>リトライ可能かどうか</returns>
    private bool IsRetryableError(ExecutionResult result)
    {
        if (string.IsNullOrEmpty(result.Error))
        {
            return false;
        }

        var error = result.Error.ToLower();

        // ネットワーク関連エラー
        if (error.Contains("network") || error.Contains("timeout") ||
            error.Contains("connection") || error.Contains("unreachable"))
        {
            return true;
        }

        // 一時的なリソース不足
        if (error.Contains("busy") || error.Contains("temporary") ||
            error.Contains("throttle") || error.Contains("rate limit"))
        {
            return true;
        }

        // ファイルロック関連
        if (error.Contains("locked") || error.Contains("sharing violation") ||
            error.Contains("access denied"))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 実行履歴に基づいて評価します
    /// </summary>
    /// <param name="context">エージェントコンテキスト</param>
    /// <param name="evaluation">評価結果</param>
    private void EvaluateExecutionHistory(AgentContext context, EvaluationResult evaluation)
    {
        var history = context.ExecutionHistory;

        if (history.Count == 0)
        {
            return;
        }

        // 連続失敗の検出
        var recentFailures = history.TakeLast(3).Count(r => !r.IsSuccessful);
        if (recentFailures >= 2)
        {
            evaluation.ShouldRetry = false;
            evaluation.Feedback += " 連続して失敗しています。根本原因を調査してください。";
            _logger.LogWarning("連続失敗を検出: 最近{RecentFailures}回の実行が失敗", recentFailures);
        }

        // パフォーマンス傾向の分析
        var avgDuration = history.Average(r => r.Duration.TotalMilliseconds);
        var lastDuration = history.Last().Duration.TotalMilliseconds;

        if (lastDuration > avgDuration * 2)
        {
            evaluation.Feedback += " 実行時間が通常より大幅に長くなっています。";
            _logger.LogWarning("実行時間の異常を検出: 平均{Ms}ms vs 最新{Ms}ms",
                avgDuration, lastDuration);
        }
    }
}
