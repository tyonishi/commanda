namespace Commanda.Core;

/// <summary>
/// エージェントオーケストレーターの実装
/// </summary>
public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly ILlmProviderManager _llmManager;
    private readonly IMcpServer _mcpServer;
    private readonly InputValidator _inputValidator;
    private readonly CancellationTokenSource _cancellationSource;
    private AgentContext? _currentContext;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="llmManager">LLMプロバイダーマネージャー</param>
    /// <param name="mcpServer">MCPサーバー</param>
    /// <param name="inputValidator">入力検証クラス</param>
    public AgentOrchestrator(ILlmProviderManager llmManager, IMcpServer mcpServer, InputValidator inputValidator)
    {
        _llmManager = llmManager ?? throw new ArgumentNullException(nameof(llmManager));
        _mcpServer = mcpServer ?? throw new ArgumentNullException(nameof(mcpServer));
        _inputValidator = inputValidator ?? throw new ArgumentNullException(nameof(inputValidator));
        _cancellationSource = new CancellationTokenSource();
    }

    /// <summary>
    /// ユーザー入力を処理してタスクを実行します
    /// </summary>
    /// <param name="userInput">ユーザーの自然言語入力</param>
    /// <returns>実行結果</returns>
    public async Task<ExecutionResult> ExecuteTaskAsync(string userInput)
    {
        _currentContext = new AgentContext { UserInput = userInput };
        var startTime = DateTime.UtcNow;

        try
        {
            // 入力検証
            var validationResult = _inputValidator.ValidateUserInput(userInput);
            if (!validationResult.IsValid)
            {
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
                    // TODO: ログに記録
                    Console.WriteLine($"警告: {warning}");
                }
            }

            // シンプルな実装：直接LLMに問い合わせ
            var provider = await _llmManager.GetActiveProviderAsync();

            var prompt = BuildSimplePrompt(userInput);
            var response = await provider.GetResponseAsync(prompt);

            return new ExecutionResult
            {
                Content = response,
                IsSuccessful = true,
                Duration = DateTime.UtcNow - startTime,
                StepsExecuted = 1
            };
        }
        catch (OperationCanceledException)
        {
            _currentContext.MarkCancelled("ユーザーによるキャンセル");
            return new ExecutionResult
            {
                Content = "実行がキャンセルされました。",
                IsSuccessful = false,
                Duration = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            return new ExecutionResult
            {
                Content = $"実行中にエラーが発生しました: {ex.Message}",
                IsSuccessful = false,
                Duration = DateTime.UtcNow - startTime
            };
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
    /// シンプルなプロンプトを構築します
    /// </summary>
    /// <param name="userInput">ユーザー入力</param>
    /// <returns>プロンプト</returns>
    private string BuildSimplePrompt(string userInput)
    {
        return $@"あなたはPC操作を支援するAIアシスタントです。

利用可能な操作:
- ファイルの読み書き
- ディレクトリの操作

ユーザーからのリクエストを理解し、適切な応答を返してください。

リクエスト: {userInput}

応答:";
    }
}
