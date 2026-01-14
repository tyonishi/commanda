using Microsoft.Extensions.Logging;
using System.Linq;

namespace Commanda.Core;

/// <summary>
/// エージェントオーケストレーターの実装
/// </summary>
public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly ITaskPlanner _taskPlanner;
    private readonly IExecutionMonitor _executionMonitor;
    private readonly IStateManager _stateManager;
    private readonly ILlmProviderManager _llmManager;
    private readonly IMcpServer _mcpServer;
    private readonly InputValidator _inputValidator;
    private readonly ILogger<AgentOrchestrator> _logger;
    private readonly CancellationTokenSource _cancellationSource;
    private AgentContext? _currentContext;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="taskPlanner">タスクプランナー</param>
    /// <param name="executionMonitor">実行モニター</param>
    /// <param name="stateManager">状態マネージャー</param>
    /// <param name="llmManager">LLMプロバイダマネージャー</param>
    /// <param name="mcpServer">MCPサーバー</param>
    /// <param name="inputValidator">入力検証クラス</param>
    /// <param name="logger">ロガー</param>
    public AgentOrchestrator(
        ITaskPlanner taskPlanner,
        IExecutionMonitor executionMonitor,
        IStateManager stateManager,
        ILlmProviderManager llmManager,
        IMcpServer mcpServer,
        InputValidator inputValidator,
        ILogger<AgentOrchestrator> logger)
    {
        _taskPlanner = taskPlanner ?? throw new ArgumentNullException(nameof(taskPlanner));
        _executionMonitor = executionMonitor ?? throw new ArgumentNullException(nameof(executionMonitor));
        _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        _llmManager = llmManager ?? throw new ArgumentNullException(nameof(llmManager));
        _mcpServer = mcpServer ?? throw new ArgumentNullException(nameof(mcpServer));
        _inputValidator = inputValidator ?? throw new ArgumentNullException(nameof(inputValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cancellationSource = new CancellationTokenSource();
    }

    /// <summary>
    /// ユーザー入力を処理してタスクを実行します
    /// </summary>
    /// <param name="userInput">ユーザーの自然言語入力</param>
    /// <returns>実行結果</returns>
    public async Task<ExecutionResult> ExecuteTaskAsync(string userInput)
    {
        var startTime = DateTime.UtcNow;
        var sessionId = GenerateSessionId(userInput, startTime);

        try
        {
            _logger.LogInformation("新しいタスク実行を開始します: {SessionId}", sessionId);

            // 入力検証
            var validationResult = _inputValidator.ValidateUserInput(userInput);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("入力検証エラー: {Error}", validationResult.ErrorMessage);
                return new ExecutionResult
                {
                    Content = $"入力検証エラー: {validationResult.ErrorMessage}",
                    IsSuccessful = false,
                    Duration = DateTime.UtcNow - startTime
                };
            }

            // 警告がある場合はログに記録
            if (validationResult.Warnings.Any())
            {
                foreach (var warning in validationResult.Warnings)
                {
                    _logger.LogWarning("入力検証警告: {Warning}", warning);
                }
            }

            // 新しいコンテキストを作成
            _currentContext = new AgentContext { UserInput = userInput };
            _currentContext.Status = ExecutionStatus.Planning;

            // 状態を保存
            await _stateManager.SaveStateAsync(_currentContext);

            // ReActループ: Planning → Execution → Evaluation → Feedback
            while (!_currentContext.IsCompleted && !_currentContext.IsCancelled)
            {
                _cancellationSource.Token.ThrowIfCancellationRequested();

                try
                {
                    // Planning Phase
                    _logger.LogInformation("Planningフェーズを開始します");
                    var plan = await _taskPlanner.GeneratePlanAsync(_currentContext, _cancellationSource.Token);
                    _currentContext.CurrentPlan = plan;
                    _currentContext.Status = ExecutionStatus.Planning;

                    // 状態を保存
                    await _stateManager.SaveStateAsync(_currentContext);

                    // Execution Phase
                    _logger.LogInformation("Executionフェーズを開始します: {StepCount}ステップ",
                        plan?.Steps.Count ?? 0);
                    _currentContext.Status = ExecutionStatus.Executing;
                    await _stateManager.SaveStateAsync(_currentContext);

                    var executionResult = await ExecutePlanAsync(plan, _cancellationSource.Token);

                    // Evaluation Phase
                    _logger.LogInformation("Evaluationフェーズを開始します");
                    _currentContext.Status = ExecutionStatus.Evaluating;
                    await _stateManager.SaveStateAsync(_currentContext);

                    var evaluation = await _executionMonitor.EvaluateResultAsync(executionResult, _currentContext);

                    if (evaluation.ShouldRetry)
                    {
                        _logger.LogInformation("リトライを決定しました: {Feedback}", evaluation.Feedback);
                        _currentContext.AddFeedback(evaluation.Feedback);

                        // 状態を保存
                        await _stateManager.SaveStateAsync(_currentContext);

                        // 次のイテレーションへ
                        continue;
                    }
                    else if (evaluation.IsSuccessful)
                    {
                        _logger.LogInformation("タスクが正常に完了しました");
                        _currentContext.MarkCompleted();

                        return new ExecutionResult
                        {
                            Content = executionResult.Content,
                            IsSuccessful = true,
                            Duration = DateTime.UtcNow - startTime,
                            StepsExecuted = _currentContext.ExecutionHistory.Count
                        };
                    }
                    else
                    {
                        _logger.LogWarning("タスクが失敗しました: {Reason}", evaluation.Reason);
                        _currentContext.MarkCancelled(evaluation.Reason);

                        return new ExecutionResult
                        {
                            Content = $"タスクが失敗しました: {evaluation.Reason}",
                            IsSuccessful = false,
                            Duration = DateTime.UtcNow - startTime,
                            StepsExecuted = _currentContext.ExecutionHistory.Count
                        };
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("実行がキャンセルされました");
                    _currentContext.MarkCancelled("ユーザーによるキャンセル");
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "実行ループ内で例外が発生しました");

                    // エラーを評価してリトライ判断
                    var errorResult = new ExecutionResult
                    {
                        Content = ex.Message,
                        IsSuccessful = false,
                        Error = ex.Message,
                        Duration = DateTime.UtcNow - startTime,
                        StepsExecuted = _currentContext.ExecutionHistory.Count
                    };

                    var evaluation = await _executionMonitor.EvaluateResultAsync(errorResult, _currentContext);

                    if (evaluation.ShouldRetry)
                    {
                        _logger.LogInformation("エラー後もリトライを決定しました: {Feedback}", evaluation.Feedback);
                        _currentContext.AddFeedback($"エラーが発生しました: {ex.Message}. {evaluation.Feedback}");

                        // 状態を保存
                        await _stateManager.SaveStateAsync(_currentContext);

                        continue;
                    }
                    else
                    {
                        _currentContext.MarkCancelled($"エラー: {ex.Message}");
                        throw;
                    }
                }
            }

            // ループ終了時の最終結果
            return new ExecutionResult
            {
                Content = _currentContext.GetFinalResponse(),
                IsSuccessful = _currentContext.IsCompleted,
                Duration = DateTime.UtcNow - startTime,
                StepsExecuted = _currentContext.ExecutionHistory.Count
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("タスク実行がキャンセルされました: {SessionId}", sessionId);
            return new ExecutionResult
            {
                Content = "実行がキャンセルされました。",
                IsSuccessful = false,
                Duration = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "タスク実行中に予期しないエラーが発生しました: {SessionId}", sessionId);
            return new ExecutionResult
            {
                Content = $"予期しないエラーが発生しました: {ex.Message}",
                IsSuccessful = false,
                Duration = DateTime.UtcNow - startTime
            };
        }
        finally
        {
            // 最終状態を保存
            if (_currentContext != null)
            {
                await _stateManager.SaveStateAsync(_currentContext);
            }
        }
    }

    /// <summary>
    /// 実行中のタスクをキャンセルします
    /// </summary>
    /// <returns>キャンセル処理のタスク</returns>
    public Task CancelExecutionAsync()
    {
        _cancellationSource.Cancel();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 現在の実行状態を取得します
    /// </summary>
    /// <returns>現在の実行状態</returns>
    public ExecutionStatus GetCurrentStatus()
    {
        return _currentContext?.Status ?? ExecutionStatus.Idle;
    }

    /// <summary>
    /// 実行計画を実行します
    /// </summary>
    /// <param name="plan">実行計画</param>
    /// <param name="token">キャンセレーショントークン</param>
    /// <returns>実行結果</returns>
    private async Task<ExecutionResult> ExecutePlanAsync(ExecutionPlan? plan, CancellationToken token)
    {
        if (plan == null || plan.Steps.Count == 0)
        {
            return new ExecutionResult
            {
                Content = "実行するステップがありません。",
                IsSuccessful = false
            };
        }

        var stepResults = new List<ExecutionResult>();

        foreach (var step in plan.Steps)
        {
            token.ThrowIfCancellationRequested();

            try
            {
                _logger.LogInformation("ステップを実行します: {ToolName}", step.ToolName);

                var stepResult = await _mcpServer.ExecuteToolAsync(
                    step.ToolName,
                    step.Arguments,
                    step.Timeout);

                var executionResult = new ExecutionResult
                {
                    Content = stepResult.Output?.ToString() ?? "実行完了",
                    IsSuccessful = stepResult.IsSuccessful,
                    Error = stepResult.Error,
                    Duration = stepResult.Duration
                };

                stepResults.Add(executionResult);
                _currentContext?.AddExecutionResult(executionResult);

                // 状態を保存
                if (_currentContext != null)
                {
                    await _stateManager.SaveStateAsync(_currentContext);
                }

                if (!stepResult.IsSuccessful)
                {
                    _logger.LogWarning("ステップ実行に失敗しました: {ToolName}, {Error}",
                        step.ToolName, stepResult.Error);
                    break; // 失敗したら停止
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("ステップ実行がキャンセルされました: {ToolName}", step.ToolName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ステップ実行中に例外が発生しました: {ToolName}", step.ToolName);

                var errorResult = new ExecutionResult
                {
                    Content = ex.Message,
                    IsSuccessful = false,
                    Error = ex.Message,
                    Duration = TimeSpan.Zero
                };

                stepResults.Add(errorResult);
                _currentContext?.AddExecutionResult(errorResult);
                break;
            }
        }

        // 最終結果を集約
        var isSuccessful = stepResults.All(r => r.IsSuccessful);
        var content = GenerateExecutionSummary(stepResults);
        var totalDuration = stepResults.Sum(r => r.Duration.TotalMilliseconds);

        return new ExecutionResult
        {
            Content = content,
            IsSuccessful = isSuccessful,
            Error = stepResults.FirstOrDefault(r => !r.IsSuccessful)?.Error,
            Duration = TimeSpan.FromMilliseconds(totalDuration),
            StepsExecuted = stepResults.Count
        };
    }

    /// <summary>
    /// 実行結果のサマリーを生成します
    /// </summary>
    /// <param name="results">ステップ実行結果</param>
    /// <returns>サマリー文字列</returns>
    private string GenerateExecutionSummary(List<ExecutionResult> results)
    {
        var successful = results.Count(r => r.IsSuccessful);
        var total = results.Count;

        if (successful == total)
        {
            return $"すべてのステップが正常に完了しました ({successful}/{total})";
        }
        else
        {
            var failedResult = results.First(r => !r.IsSuccessful);
            return $"ステップ実行で失敗しました: {failedResult.Error}";
        }
    }

    /// <summary>
    /// セッションIDを生成します
    /// </summary>
    /// <param name="userInput">ユーザー入力</param>
    /// <param name="startTime">開始時間</param>
    /// <returns>セッションID</returns>
    private string GenerateSessionId(string userInput, DateTime startTime)
    {
        var input = userInput ?? "unknown";
        var time = startTime.ToString("yyyyMMddHHmmss");
        var hash = input.GetHashCode().ToString("X8");
        return $"{time}_{hash}";
    }
}
