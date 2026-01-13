using System.Text.Json;

namespace Commanda.Core;

/// <summary>
/// タスクプランナーの実装
/// </summary>
public class TaskPlanner : ITaskPlanner
{
    private readonly ILlmProviderManager _llmManager;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="llmManager">LLMプロバイダマネージャー</param>
    public TaskPlanner(ILlmProviderManager llmManager)
    {
        _llmManager = llmManager ?? throw new ArgumentNullException(nameof(llmManager));
    }

    /// <summary>
    /// 計画レスポンス
    /// </summary>
    private class PlanResponse
    {
        public string? Description { get; set; }
        public List<PlanStep>? Steps { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
    }

    /// <summary>
    /// 計画ステップ
    /// </summary>
    private class PlanStep
    {
        public string? ToolName { get; set; }
        public Dictionary<string, object>? Arguments { get; set; }
        public string? ExpectedOutcome { get; set; }
        public int Timeout { get; set; }
    }

    /// <summary>
    /// 実行計画を生成します
    /// </summary>
    /// <param name="context">エージェントコンテキスト</param>
    /// <param name="token">キャンセレーショントークン</param>
    /// <returns>実行計画</returns>
    public async Task<ExecutionPlan> GeneratePlanAsync(AgentContext context, CancellationToken token = default)
    {
        var provider = await _llmManager.GetActiveProviderAsync();

        // 計画生成プロンプトの構築
        var prompt = BuildPlanningPrompt(context);

        // LLMに計画を問い合わせ
        var response = provider.StreamResponseAsync(
            prompt,
            ResponseFormat.JSON,
            token);

        // ストリームを読み取り
        var chunks = new List<string>();
        await foreach (var chunk in response)
        {
            chunks.Add(chunk);
        }
        var fullResponse = string.Join("", chunks);

        // レスポンスをパースしてExecutionPlanに変換
        return ParsePlanFromResponse(fullResponse);
    }

    /// <summary>
    /// 計画生成プロンプトを構築します
    /// </summary>
    /// <param name="context">エージェントコンテキスト</param>
    /// <returns>プロンプト文字列</returns>
    private string BuildPlanningPrompt(AgentContext context)
    {
        var systemPrompt = @"あなたはPC操作を自動化するアシスタントです。
ユーザーの自然言語リクエストを分析し、実行可能なステップに分解してください。

利用可能なツール:
- FileOperations: ファイルの作成、移動、コピー、削除
- ApplicationControl: アプリケーションの起動、終了、制御
- TextProcessing: テキストファイルの読み書き、編集

応答形式は以下のJSON形式で:
{
  ""description"": ""計画の概要"",
  ""steps"": [
    {
      ""toolName"": ""ツール名"",
      ""arguments"": {""パラメータ"": ""値""},
      ""expectedOutcome"": ""期待される結果"",
      ""timeout"": 30
    }
  ],
  ""parameters"": {""追加設定"": ""値""}
}";

        var userContext = "";
        if (context.FeedbackHistory.Any())
        {
            userContext += $"\n以前のフィードバック: {string.Join(", ", context.FeedbackHistory)}\n";
        }

        return $"{systemPrompt}\n\nユーザーリクエスト: {context.UserInput}{userContext}";
    }

    /// <summary>
    /// レスポンスから計画をパースします
    /// </summary>
    /// <param name="response">LLMレスポンス</param>
    /// <returns>実行計画</returns>
    private Commanda.Core.ExecutionPlan ParsePlanFromResponse(string response)
    {
        try
        {
            // JSONレスポンスをパース
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var planData = JsonSerializer.Deserialize<TaskPlanner.PlanResponse>(response, options);

            var executionPlan = new Commanda.Core.ExecutionPlan();
            executionPlan.Description = planData?.Description ?? "計画が生成されました";

            if (planData?.Steps != null)
            {
                executionPlan.Steps = planData.Steps.Select(s => new Commanda.Core.ExecutionStep
                {
                    ToolName = s.ToolName ?? string.Empty,
                    Arguments = s.Arguments ?? new Dictionary<string, object>(),
                    ExpectedOutcome = s.ExpectedOutcome ?? string.Empty,
                    Timeout = TimeSpan.FromSeconds(s.Timeout)
                }).ToList();
            }
            else
            {
                executionPlan.Steps = new List<Commanda.Core.ExecutionStep>();
            }

            executionPlan.Parameters = planData?.Parameters ?? new Dictionary<string, object>();

            return executionPlan;
        }
        catch (Exception ex)
        {
            throw new PlanningException($"計画の解析に失敗しました: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// 計画生成例外
/// </summary>
public class PlanningException : CommandaException
{
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="message">メッセージ</param>
    /// <param name="inner">内部例外</param>
    public PlanningException(string message, Exception? inner = null)
        : base(message, "PLANNING_ERROR", inner != null ? new Dictionary<string, object> { ["inner"] = inner } : new Dictionary<string, object>())
    {
    }
}