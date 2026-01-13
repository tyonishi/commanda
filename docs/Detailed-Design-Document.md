# Commanda 詳細設計書

## 1. システム構成要素

### 1.1 Desktop Client

#### 1.1.1 クラス構成
```csharp
// MainWindow.xaml.cs
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}

// MainViewModel.cs
public class MainViewModel : INotifyPropertyChanged
{
    private readonly IAgentOrchestrator _agentOrchestrator;
    private readonly ILlmProviderManager _llmManager;
    
    public ObservableCollection<MessageViewModel> Messages { get; }
    public string CurrentMessage { get; set; }
    public bool IsExecuting { get; set; }
    
    public DelegateCommand SendCommand { get; }
    
    public MainViewModel(
        IAgentOrchestrator agentOrchestrator,
        ILlmProviderManager llmManager)
    {
        _agentOrchestrator = agentOrchestrator;
        _llmManager = llmManager;
        
        Messages = new ObservableCollection<MessageViewModel>();
        SendCommand = new DelegateCommand(
            async () => await SendMessageAsync(),
            () => !string.IsNullOrWhiteSpace(CurrentMessage) && !IsExecuting);
    }
    
    private async Task SendMessageAsync()
    {
        var userMessage = new MessageViewModel
        {
            SenderName = "あなた",
            SenderInitial = "U",
            Content = CurrentMessage,
            Timestamp = DateTime.Now
        };
        
        Messages.Add(userMessage);
        var messageToSend = CurrentMessage;
        CurrentMessage = "";
        
        IsExecuting = true;
        
        try
        {
            var result = await _agentOrchestrator.ExecuteTaskAsync(messageToSend);
            
            var aiMessage = new MessageViewModel
            {
                SenderName = "Commanda",
                SenderInitial = "C",
                Content = result.Content,
                Timestamp = DateTime.Now
            };
            
            Messages.Add(aiMessage);
        }
        catch (Exception ex)
        {
            var errorMessage = new MessageViewModel
            {
                SenderName = "システム",
                SenderInitial = "S",
                Content = $"エラーが発生しました: {ex.Message}",
                Timestamp = DateTime.Now
            };
            
            Messages.Add(errorMessage);
        }
        finally
        {
            IsExecuting = false;
        }
    }
}

// MessageViewModel.cs
public class MessageViewModel : INotifyPropertyChanged
{
    public string SenderName { get; set; }
    public string SenderInitial { get; set; }
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }
}
```

#### 1.1.2 UI仕様
```xaml
<!-- MessageTemplate.xaml -->
<DataTemplate x:Key="MessageTemplate">
    <Border Margin="16,8" Background="White" CornerRadius="8" 
           BorderBrush="#E0E0E0" BorderThickness="1" Padding="12" MaxWidth="600">
        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>
            
            <!-- アバター -->
            <Border Grid.Column="0" Width="32" Height="32" 
                   CornerRadius="16" Margin="0,0,12,0"
                   Background="{Binding SenderInitial, Converter={StaticResource InitialToColorConverter}}">
                <TextBlock Text="{Binding SenderInitial}" 
                          HorizontalAlignment="Center" VerticalAlignment="Center"
                          FontWeight="Bold" Foreground="White"/>
            </Border>
            
            <!-- メッセージ内容 -->
            <StackPanel Grid.Column="1">
                <TextBlock Text="{Binding SenderName}" FontWeight="SemiBold" Margin="0,0,0,4"/>
                <local:MarkdownTextBlock Text="{Binding Content}" FontSize="14" LineHeight="1.4"/>
                <TextBlock Text="{Binding Timestamp, StringFormat='{}{0:HH:mm}'}" 
                          FontSize="11" Foreground="#666666" Margin="0,4,0,0"/>
            </StackPanel>
        </Grid>
    </Border>
</DataTemplate>
```

### 1.2 Agent Orchestrator

#### 1.2.1 インターフェース定義
```csharp
public interface IAgentOrchestrator
{
    Task<ExecutionResult> ExecuteTaskAsync(string userInput);
    Task CancelExecutionAsync();
    ExecutionStatus GetCurrentStatus();
}

public class ExecutionResult
{
    public string Content { get; set; }
    public bool IsSuccessful { get; set; }
    public List<string> Warnings { get; set; }
    public TimeSpan Duration { get; set; }
    public int StepsExecuted { get; set; }
}

public enum ExecutionStatus
{
    Idle,
    Planning,
    Executing,
    Evaluating,
    Completed,
    Failed,
    Cancelled
}
```

#### 1.2.2 実装クラス
```csharp
public class AgentOrchestrator : IAgentOrchestrator
{
    private readonly ITaskPlanner _planner;
    private readonly IExecutionMonitor _monitor;
    private readonly IStateManager _stateManager;
    private readonly IMcpServer _mcpServer;
    private readonly CancellationTokenSource _cancellationSource;
    
    private AgentContext _currentContext;
    
    public AgentOrchestrator(
        ITaskPlanner planner,
        IExecutionMonitor monitor,
        IStateManager stateManager,
        IMcpServer mcpServer)
    {
        _planner = planner;
        _monitor = monitor;
        _stateManager = stateManager;
        _mcpServer = mcpServer;
        _cancellationSource = new CancellationTokenSource();
    }
    
    public async Task<ExecutionResult> ExecuteTaskAsync(string userInput)
    {
        _currentContext = new AgentContext { UserInput = userInput };
        var startTime = DateTime.UtcNow;
        
        try
        {
            while (!_currentContext.IsCompleted && !_currentContext.IsCancelled)
            {
                // Planning Phase
                _currentContext.Status = ExecutionStatus.Planning;
                var plan = await _planner.GeneratePlanAsync(_currentContext, _cancellationSource.Token);
                _currentContext.CurrentPlan = plan;
                
                // Execution Phase
                _currentContext.Status = ExecutionStatus.Executing;
                var executionResult = await ExecutePlanAsync(plan, _cancellationSource.Token);
                
                // Evaluation Phase
                _currentContext.Status = ExecutionStatus.Evaluating;
                var evaluation = await _monitor.EvaluateResultAsync(executionResult, _currentContext);
                
                if (evaluation.ShouldRetry)
                {
                    _currentContext.AddFeedback(evaluation.Feedback);
                    continue;
                }
                
                if (evaluation.IsSuccessful)
                {
                    _currentContext.MarkCompleted();
                }
                else
                {
                    _currentContext.MarkCancelled(evaluation.Reason);
                }
            }
            
            return new ExecutionResult
            {
                Content = _currentContext.GetFinalResponse(),
                IsSuccessful = _currentContext.IsCompleted,
                Warnings = _currentContext.GetWarnings(),
                Duration = DateTime.UtcNow - startTime,
                StepsExecuted = _currentContext.ExecutionHistory.Count
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
            _currentContext.MarkCancelled($"予期しないエラー: {ex.Message}");
            return new ExecutionResult
            {
                Content = $"実行中にエラーが発生しました: {ex.Message}",
                IsSuccessful = false,
                Duration = DateTime.UtcNow - startTime
            };
        }
    }
    
    private async Task<ExecutionResult> ExecutePlanAsync(ExecutionPlan plan, CancellationToken token)
    {
        var results = new List<StepExecutionResult>();
        
        foreach (var step in plan.Steps)
        {
            token.ThrowIfCancellationRequested();
            
            try
            {
                var stepResult = await _mcpServer.ExecuteToolAsync(
                    step.ToolName, 
                    step.Arguments, 
                    step.Timeout);
                
                results.Add(new StepExecutionResult
                {
                    Step = step,
                    IsSuccessful = stepResult.IsSuccessful,
                    Output = stepResult.Output,
                    Error = stepResult.Error
                });
                
                _currentContext.AddExecutionResult(stepResult);
                
                if (!stepResult.IsSuccessful)
                {
                    break; // 失敗したら停止
                }
            }
            catch (Exception ex)
            {
                results.Add(new StepExecutionResult
                {
                    Step = step,
                    IsSuccessful = false,
                    Error = ex.Message
                });
                
                break;
            }
        }
        
        return new ExecutionResult
        {
            IsSuccessful = results.All(r => r.IsSuccessful),
            Content = GenerateExecutionSummary(results),
            Warnings = results.Where(r => !string.IsNullOrEmpty(r.Error))
                             .Select(r => r.Error).ToList()
        };
    }
    
    private string GenerateExecutionSummary(List<StepExecutionResult> results)
    {
        var successful = results.Count(r => r.IsSuccessful);
        var total = results.Count;
        
        if (successful == total)
        {
            return $"すべてのステップが正常に完了しました ({successful}/{total})";
        }
        else
        {
            var failedStep = results.First(r => !r.IsSuccessful);
            return $"ステップ {failedStep.Step.ToolName} で失敗しました: {failedStep.Error}";
        }
    }
    
    public Task CancelExecutionAsync()
    {
        _cancellationSource.Cancel();
        return Task.CompletedTask;
    }
    
    public ExecutionStatus GetCurrentStatus()
    {
        return _currentContext?.Status ?? ExecutionStatus.Idle;
    }
}
```

#### 1.2.3 AgentContextクラス
```csharp
public class AgentContext
{
    public string UserInput { get; set; }
    public ExecutionPlan CurrentPlan { get; set; }
    public ExecutionStatus Status { get; set; }
    public List<ExecutionResult> ExecutionHistory { get; } = new();
    public List<string> FeedbackHistory { get; } = new();
    public bool IsCompleted { get; private set; }
    public bool IsCancelled { get; private set; }
    public string CancellationReason { get; private set; }
    public DateTime StartedAt { get; } = DateTime.UtcNow;
    
    public void AddExecutionResult(ExecutionResult result)
    {
        ExecutionHistory.Add(result);
    }
    
    public void AddFeedback(string feedback)
    {
        FeedbackHistory.Add(feedback);
    }
    
    public void MarkCompleted()
    {
        IsCompleted = true;
        Status = ExecutionStatus.Completed;
    }
    
    public void MarkCancelled(string reason)
    {
        IsCancelled = true;
        CancellationReason = reason;
        Status = ExecutionStatus.Cancelled;
    }
    
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
    
    public List<string> GetWarnings()
    {
        return ExecutionHistory
            .Where(r => !string.IsNullOrEmpty(r.Error))
            .Select(r => r.Error)
            .ToList();
    }
}
```

### 1.3 Task Planner

#### 1.3.1 インターフェースと実装
```csharp
public interface ITaskPlanner
{
    Task<ExecutionPlan> GeneratePlanAsync(AgentContext context, CancellationToken token = default);
}

public class TaskPlanner : ITaskPlanner
{
    private readonly ILlmProviderManager _llmManager;
    private readonly IPromptManager _promptManager;
    
    public TaskPlanner(ILlmProviderManager llmManager, IPromptManager promptManager)
    {
        _llmManager = llmManager;
        _promptManager = promptManager;
    }
    
    public async Task<ExecutionPlan> GeneratePlanAsync(AgentContext context, CancellationToken token = default)
    {
        var provider = await _llmManager.GetActiveProviderAsync();
        
        // 計画生成プロンプトの構築
        var prompt = BuildPlanningPrompt(context);
        
        // LLMに計画を問い合わせ
        var response = await provider.StreamResponseAsync(
            prompt,
            ResponseFormat.JSON,
            token);
        
        // レスポンスをパースしてExecutionPlanに変換
        return ParsePlanFromResponse(await response.ReadToEndAsync());
    }
    
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
    
    private ExecutionPlan ParsePlanFromResponse(string response)
    {
        try
        {
            // JSONレスポンスをパース
            var planData = JsonSerializer.Deserialize<PlanResponse>(response);
            
            return new ExecutionPlan
            {
                Description = planData.Description,
                Steps = planData.Steps.Select(s => new ExecutionStep
                {
                    ToolName = s.ToolName,
                    Arguments = s.Arguments,
                    ExpectedOutcome = s.ExpectedOutcome,
                    Timeout = TimeSpan.FromSeconds(s.Timeout)
                }).ToList(),
                Parameters = planData.Parameters ?? new Dictionary<string, object>()
            };
        }
        catch (Exception ex)
        {
            throw new PlanningException($"計画の解析に失敗しました: {ex.Message}", ex);
        }
    }
}

public class PlanResponse
{
    public string Description { get; set; }
    public List<PlanStep> Steps { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
}

public class PlanStep
{
    public string ToolName { get; set; }
    public Dictionary<string, object> Arguments { get; set; }
    public string ExpectedOutcome { get; set; }
    public int Timeout { get; set; }
}
```

### 1.4 MCP Server

#### 1.4.1 インターフェース定義
```csharp
public interface IMcpServer
{
    Task InitializeAsync();
    Task<ToolResult> ExecuteToolAsync(string toolName, Dictionary<string, object> arguments, TimeSpan timeout);
    Task<List<string>> GetAvailableToolsAsync();
    Task<bool> RegisterExtensionAsync(IMcpExtension extension);
    Task<bool> UnregisterExtensionAsync(string extensionName);
}

public class ToolResult
{
    public bool IsSuccessful { get; set; }
    public object Output { get; set; }
    public string Error { get; set; }
    public TimeSpan Duration { get; set; }
}
```

#### 1.4.2 実装クラス
```csharp
public class McpServer : IMcpServer
{
    private readonly ILogger<McpServer> _logger;
    private readonly IExtensionManager _extensionManager;
    private readonly Dictionary<string, McpServerToolType> _toolTypes;
    private McpServer _mcpServerInstance;
    
    public McpServer(ILogger<McpServer> logger, IExtensionManager extensionManager)
    {
        _logger = logger;
        _extensionManager = extensionManager;
        _toolTypes = new Dictionary<string, McpServerToolType>();
    }
    
    public async Task InitializeAsync()
    {
        _logger.LogInformation("MCPサーバーを初期化しています...");
        
        // 拡張機能をロード
        await _extensionManager.LoadExtensionsAsync();
        
        // MCPサーバーインスタンスを作成
        var options = new McpServerOptions
        {
            ServerInfo = new Implementation { Name = "Commanda", Version = "1.0.0" }
        };
        
        // ツールハンドラーの設定
        options.Handlers.ListToolsHandler = HandleListTools;
        options.Handlers.CallToolHandler = HandleCallTool;
        
        _mcpServerInstance = McpServer.Create(new StdioServerTransport("Commanda"), options);
        
        _logger.LogInformation("MCPサーバーの初期化が完了しました");
    }
    
    private async Task<ListToolsResult> HandleListTools(ListToolsRequest request, CancellationToken token)
    {
        var tools = new List<Tool>();
        
        // 組み込みツールの追加
        tools.AddRange(GetBuiltInTools());
        
        // 拡張ツールの追加
        var extensions = await _extensionManager.GetLoadedExtensionsAsync();
        foreach (var extension in extensions)
        {
            foreach (var toolType in extension.ToolTypes)
            {
                var toolInfo = ExtractToolInfo(toolType);
                tools.Add(toolInfo);
            }
        }
        
        return new ListToolsResult { Tools = tools };
    }
    
    private async Task<CallToolResult> HandleCallTool(CallToolRequest request, CancellationToken token)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogInformation($"ツール実行: {request.Params.Name}");
            
            var result = await ExecuteToolInternalAsync(
                request.Params.Name, 
                request.Params.Arguments, 
                token);
            
            var duration = DateTime.UtcNow - startTime;
            
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock 
                    { 
                        Text = result.Output?.ToString() ?? "実行完了",
                        Type = "text" 
                    }
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"ツール実行エラー: {request.Params.Name}");
            
            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock 
                    { 
                        Text = $"エラー: {ex.Message}",
                        Type = "text" 
                    }
                },
                IsError = true
            };
        }
    }
    
    private async Task<ToolResult> ExecuteToolInternalAsync(
        string toolName, 
        Dictionary<string, object> arguments, 
        CancellationToken token)
    {
        // 拡張ツールの検索
        var extensions = await _extensionManager.GetLoadedExtensionsAsync();
        foreach (var extension in extensions)
        {
            var toolType = extension.ToolTypes.FirstOrDefault(t => 
                GetToolNameFromType(t) == toolName);
            
            if (toolType != null)
            {
                return await ExecuteExtensionToolAsync(toolType, arguments, token);
            }
        }
        
        // 組み込みツールの実行
        return await ExecuteBuiltInToolAsync(toolName, arguments, token);
    }
    
    private async Task<ToolResult> ExecuteExtensionToolAsync(
        McpServerToolType toolType, 
        Dictionary<string, object> arguments, 
        CancellationToken token)
    {
        // リフレクションを使ってツールメソッドを実行
        var methods = toolType.GetMethods()
            .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null);
        
        foreach (var method in methods)
        {
            var toolAttr = method.GetCustomAttribute<McpServerToolAttribute>();
            if (toolAttr.Name == toolName)
            {
                var instance = Activator.CreateInstance(toolType);
                var result = await (Task)method.Invoke(instance, new object[] { arguments, token });
                return new ToolResult { IsSuccessful = true, Output = result };
            }
        }
        
        throw new ToolNotFoundException($"ツール '{toolName}' が見つかりません");
    }
    
    private async Task<ToolResult> ExecuteBuiltInToolAsync(
        string toolName, 
        Dictionary<string, object> arguments, 
        CancellationToken token)
    {
        // 組み込みツールの実装（例: ファイル操作）
        switch (toolName)
        {
            case "read_file":
                return await FileOperations.ReadFileAsync(arguments, token);
            case "write_file":
                return await FileOperations.WriteFileAsync(arguments, token);
            case "list_directory":
                return await FileOperations.ListDirectoryAsync(arguments, token);
            default:
                throw new ToolNotFoundException($"組み込みツール '{toolName}' が見つかりません");
        }
    }
    
    private List<Tool> GetBuiltInTools()
    {
        return new List<Tool>
        {
            new Tool
            {
                Name = "read_file",
                Description = "指定されたファイルを読み込みます",
                InputSchema = JsonSerializer.Deserialize<JsonElement>("""
                {
                    "type": "object",
                    "properties": {
                        "path": { "type": "string", "description": "ファイルパス" }
                    },
                    "required": ["path"]
                }
                """)
            },
            new Tool
            {
                Name = "write_file",
                Description = "指定されたファイルにテキストを書き込みます",
                InputSchema = JsonSerializer.Deserialize<JsonElement>("""
                {
                    "type": "object",
                    "properties": {
                        "path": { "type": "string", "description": "ファイルパス" },
                        "content": { "type": "string", "description": "書き込む内容" }
                    },
                    "required": ["path", "content"]
                }
                """)
            },
            new Tool
            {
                Name = "list_directory",
                Description = "指定されたディレクトリの内容を一覧表示します",
                InputSchema = JsonSerializer.Deserialize<JsonElement>("""
                {
                    "type": "object",
                    "properties": {
                        "path": { "type": "string", "description": "ディレクトリパス" }
                    },
                    "required": ["path"]
                }
                """)
            }
        };
    }
    
    public async Task<List<string>> GetAvailableToolsAsync()
    {
        var tools = await HandleListTools(new ListToolsRequest(), CancellationToken.None);
        return tools.Tools.Select(t => t.Name).ToList();
    }
    
    public async Task<bool> RegisterExtensionAsync(IMcpExtension extension)
    {
        return await _extensionManager.RegisterExtensionAsync(extension);
    }
    
    public async Task<bool> UnregisterExtensionAsync(string extensionName)
    {
        return await _extensionManager.UnregisterExtensionAsync(extensionName);
    }
}
```

### 1.5 データモデル

#### 1.5.1 データベーススキーマ
```sql
-- SQLiteスキーマ
CREATE TABLE ExecutionLogs (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Timestamp DATETIME NOT NULL,
    UserId TEXT,
    TaskDescription TEXT,
    Status TEXT,
    Result TEXT,
    Duration TEXT,
    StepsExecuted INTEGER,
    ErrorMessage TEXT
);

CREATE TABLE TaskHistories (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SessionId TEXT NOT NULL,
    UserInput TEXT,
    ExecutionPlan TEXT, -- JSON
    StartTime DATETIME,
    EndTime DATETIME,
    Status TEXT,
    FinalResult TEXT
);

CREATE TABLE Extensions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT UNIQUE NOT NULL,
    Version TEXT,
    AssemblyPath TEXT,
    IsEnabled BOOLEAN DEFAULT 1,
    InstalledAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    LastUsed DATETIME
);

CREATE TABLE LlmProviders (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT UNIQUE NOT NULL,
    ProviderType TEXT,
    ApiKey TEXT, -- 暗号化済み
    BaseUri TEXT,
    ModelName TEXT,
    IsDefault BOOLEAN DEFAULT 0,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    LastValidated DATETIME
);
```

#### 1.5.2 Entity Frameworkモデル
```csharp
public class ExecutionLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string? UserId { get; set; }
    public string TaskDescription { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Result { get; set; }
    public TimeSpan Duration { get; set; }
    public int StepsExecuted { get; set; }
    public string? ErrorMessage { get; set; }
}

public class TaskHistory
{
    public int Id { get; set; }
    public string SessionId { get; set; } = "";
    public string UserInput { get; set; } = "";
    public string ExecutionPlan { get; set; } = ""; // JSON
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string Status { get; set; } = "";
    public string? FinalResult { get; set; }
}

public class ExtensionInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string AssemblyPath { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
    public DateTime InstalledAt { get; set; }
    public DateTime? LastUsed { get; set; }
}

public class LlmProviderConfig
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string ProviderType { get; set; } = "";
    public string ApiKey { get; set; } = ""; // 暗号化済み
    public string BaseUri { get; set; } = "";
    public string ModelName { get; set; } = "";
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastValidated { get; set; }
}
```

### 1.6 API仕様

#### 1.6.1 REST API (設定管理用)
```csharp
// Settings API
[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsManager _settingsManager;
    
    public SettingsController(ISettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
    }
    
    [HttpGet("llm")]
    public async Task<ActionResult<LlmSettings>> GetLlmSettings()
    {
        var settings = await _settingsManager.LoadLlmSettingsAsync();
        return Ok(settings);
    }
    
    [HttpPost("llm/providers")]
    public async Task<ActionResult<LlmProviderConfig>> AddProvider([FromBody] LlmProviderConfig config)
    {
        var added = await _settingsManager.AddProviderAsync(config);
        return CreatedAtAction(nameof(GetProvider), new { name = added.Name }, added);
    }
    
    [HttpDelete("llm/providers/{name}")]
    public async Task<IActionResult> RemoveProvider(string name)
    {
        var removed = await _settingsManager.RemoveProviderAsync(name);
        return removed ? NoContent() : NotFound();
    }
    
    [HttpPost("llm/providers/{name}/test")]
    public async Task<ActionResult<bool>> TestProvider(string name)
    {
        var settings = await _settingsManager.LoadLlmSettingsAsync();
        var provider = settings.GetProvider(name);
        if (provider == null) return NotFound();
        
        var result = await _settingsManager.TestProviderAsync(provider);
        return Ok(result);
    }
}

// Extensions API
[ApiController]
[Route("api/extensions")]
public class ExtensionsController : ControllerBase
{
    private readonly IExtensionManager _extensionManager;
    
    public ExtensionsController(IExtensionManager extensionManager)
    {
        _extensionManager = extensionManager;
    }
    
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ExtensionInfo>>> GetExtensions()
    {
        var extensions = await _extensionManager.GetLoadedExtensionsAsync();
        return Ok(extensions.Select(e => new ExtensionInfo
        {
            Name = e.Name,
            Version = e.Version
        }));
    }
    
    [HttpPost("reload")]
    public async Task<IActionResult> ReloadExtensions()
    {
        await _extensionManager.ReloadExtensionsAsync();
        return Ok();
    }
}
```

#### 1.6.2 MCPプロトコル仕様
```json
// ListToolsリクエスト
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/list",
  "params": {}
}

// ListToolsレスポンス
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "tools": [
      {
        "name": "read_file",
        "description": "指定されたファイルを読み込みます",
        "inputSchema": {
          "type": "object",
          "properties": {
            "path": { "type": "string", "description": "ファイルパス" }
          },
          "required": ["path"]
        }
      }
    ]
  }
}

// CallToolリクエスト
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "read_file",
    "arguments": {
      "path": "/path/to/file.txt"
    }
  }
}

// CallToolレスポンス
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "ファイルの内容..."
      }
    ]
  }
}
```

### 1.7 エラー処理

#### 1.7.1 例外クラス
```csharp
public class CommandaException : Exception
{
    public string ErrorCode { get; }
    public Dictionary<string, object> Context { get; }
    
    public CommandaException(string message, string errorCode = null, 
                           Dictionary<string, object> context = null) 
        : base(message)
    {
        ErrorCode = errorCode ?? "GENERAL_ERROR";
        Context = context ?? new Dictionary<string, object>();
    }
}

public class PlanningException : CommandaException
{
    public PlanningException(string message, Exception inner = null) 
        : base(message, "PLANNING_ERROR", null)
    {
        // Planning specific logic
    }
}

public class ToolNotFoundException : CommandaException
{
    public ToolNotFoundException(string toolName) 
        : base($"ツール '{toolName}' が見つかりません", "TOOL_NOT_FOUND", 
               new Dictionary<string, object> { ["toolName"] = toolName })
    {
    }
}

public class LlmProviderException : CommandaException
{
    public LlmProviderException(string message, string providerName) 
        : base(message, "LLM_PROVIDER_ERROR",
               new Dictionary<string, object> { ["provider"] = providerName })
    {
    }
}
```

#### 1.7.2 グローバル例外ハンドラー
```csharp
public class GlobalExceptionHandler : IObserver<Exception>
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }
    
    public void OnNext(Exception ex)
    {
        _logger.LogError(ex, "未処理の例外が発生しました");
        
        // UIにエラーを通知
        if (ex is CommandaException commandaEx)
        {
            ShowUserFriendlyError(commandaEx);
        }
        else
        {
            ShowGenericError(ex);
        }
    }
    
    private void ShowUserFriendlyError(CommandaException ex)
    {
        var message = GetUserFriendlyMessage(ex.ErrorCode);
        
        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
        });
    }
    
    private string GetUserFriendlyMessage(string errorCode)
    {
        return errorCode switch
        {
            "TOOL_NOT_FOUND" => "指定された操作が見つかりません。拡張機能が正しくインストールされているか確認してください。",
            "LLM_PROVIDER_ERROR" => "AIプロバイダとの通信に失敗しました。ネットワーク接続とAPIキーを確認してください。",
            "PLANNING_ERROR" => "タスクの計画作成に失敗しました。入力をより具体的にしてください。",
            _ => "予期しないエラーが発生しました。アプリケーションを再起動してください。"
        };
    }
    
    private void ShowGenericError(Exception ex)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(
                "予期しないエラーが発生しました。詳細はログを確認してください。",
                "エラー", 
                MessageBoxButton.OK, 
                MessageBoxImage.Error);
        });
    }
    
    public void OnError(Exception error) { }
    public void OnCompleted() { }
}
```

## 2. テスト仕様

### 2.1 ユニットテスト
```csharp
[TestFixture]
public class AgentOrchestratorTests
{
    private Mock<ITaskPlanner> _plannerMock;
    private Mock<IExecutionMonitor> _monitorMock;
    private Mock<IStateManager> _stateMock;
    private Mock<IMcpServer> _mcpMock;
    
    [SetUp]
    public void Setup()
    {
        _plannerMock = new Mock<ITaskPlanner>();
        _monitorMock = new Mock<IExecutionMonitor>();
        _stateMock = new Mock<IStateManager>();
        _mcpMock = new Mock<IMcpServer>();
    }
    
    [Test]
    public async Task ExecuteTaskAsync_SuccessfulExecution_ReturnsResult()
    {
        // Arrange
        var orchestrator = new AgentOrchestrator(
            _plannerMock.Object, _monitorMock.Object, 
            _stateMock.Object, _mcpMock.Object);
        
        var plan = new ExecutionPlan { Description = "Test plan" };
        var evalResult = new EvaluationResult { IsSuccessful = true };
        
        _plannerMock.Setup(p => p.GeneratePlanAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(plan);
        _monitorMock.Setup(m => m.EvaluateResultAsync(It.IsAny<ExecutionResult>(), It.IsAny<AgentContext>()))
                   .ReturnsAsync(evalResult);
        
        // Act
        var result = await orchestrator.ExecuteTaskAsync("test input");
        
        // Assert
        Assert.IsTrue(result.IsSuccessful);
        _plannerMock.Verify(p => p.GeneratePlanAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### 2.2 統合テスト
```csharp
[TestFixture]
public class AgentExecutionIntegrationTests
{
    private IServiceProvider _services;
    
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        // テスト用のDIコンテナ設定
        var services = new ServiceCollection();
        // ... サービス登録
        
        _services = services.BuildServiceProvider();
    }
    
    [Test]
    public async Task FullExecutionFlow_FileOperation_Succeeds()
    {
        // Arrange
        var orchestrator = _services.GetRequiredService<IAgentOrchestrator>();
        
        // Act
        var result = await orchestrator.ExecuteTaskAsync(
            "test.txtというファイルを作成して「Hello World」と書き込んでください");
        
        // Assert
        Assert.IsTrue(result.IsSuccessful);
        Assert.IsTrue(File.Exists("test.txt"));
        Assert.AreEqual("Hello World", File.ReadAllText("test.txt"));
        
        // Cleanup
        File.Delete("test.txt");
    }
}
```

## 3. デプロイメント仕様

### 3.1 ビルド構成
```xml
<!-- Commanda.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <ApplicationIcon>Resources\Commanda.ico</ApplicationIcon>
    <AssemblyTitle>Commanda</AssemblyTitle>
    <AssemblyDescription>AI-powered PC automation tool</AssemblyDescription>
    <Version>1.0.0</Version>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="ModelContextProtocol" Version="0.5.0-preview.1" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />
    <PackageReference Include="MaterialDesignThemes" Version="4.9.0" />
    <PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
  </ItemGroup>
</Project>
```

### 3.2 インストーラー設定
```xml
<!-- Product.wxs (WiX Toolset) -->
<Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
  <Product Id="*" Name="Commanda" Language="1033" Version="1.0.0.0" 
           Manufacturer="Commanda Team" UpgradeCode="YOUR-GUID-HERE">
    
    <Package InstallerVersion="200" Compressed="yes" />
    
    <Media Id="1" Cabinet="Commanda.cab" EmbedCab="yes" />
    
    <Directory Id="TARGETDIR" Name="SourceDir">
      <Directory Id="ProgramFilesFolder">
        <Directory Id="INSTALLFOLDER" Name="Commanda">
          <Component Id="MainExecutable" Guid="YOUR-GUID-HERE">
            <File Id="CommandaExe" Source="Commanda.exe" KeyPath="yes" />
          </Component>
          
          <!-- 設定ディレクトリ -->
          <Directory Id="AppDataFolder" Name="Commanda">
            <Component Id="SettingsFolder" Guid="YOUR-GUID-HERE">
              <CreateFolder />
            </Component>
          </Directory>
        </Directory>
      </Directory>
      
      <!-- スタートメニュー -->
      <Directory Id="ProgramMenuFolder">
        <Directory Id="ApplicationProgramsFolder" Name="Commanda">
          <Component Id="StartMenuShortcut" Guid="YOUR-GUID-HERE">
            <Shortcut Id="ApplicationStartMenuShortcut" 
                     Name="Commanda" 
                     Description="AI-powered PC automation tool"
                     Target="[INSTALLFOLDER]Commanda.exe"
                     WorkingDirectory="INSTALLFOLDER"/>
          </Component>
        </Directory>
      </Directory>
    </Directory>
    
    <Feature Id="MainFeature" Title="Commanda" Level="1">
      <ComponentRef Id="MainExecutable" />
      <ComponentRef Id="SettingsFolder" />
      <ComponentRef Id="StartMenuShortcut" />
    </Feature>
  </Product>
</Wix>
```

### 3.3 CI/CDパイプライン
```yaml
# .github/workflows/build.yml
name: Build and Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 8.0.x
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --configuration Release --no-restore
      
    - name: Run tests
      run: dotnet test --configuration Release --no-build --verbosity normal
      
    - name: Publish
      run: dotnet publish --configuration Release --runtime win-x64 --self-contained
      
    - name: Upload artifacts
      uses: actions/upload-artifact@v3
      with:
        name: commanda-win-x64
        path: ./bin/Release/net8.0-windows/win-x64/publish/
```

## 4. 結論

この詳細設計書は、Commandaの基本設計書で定義されたアーキテクチャを具体的に実装するための仕様を提供します。主要なコンポーネントの詳細なクラス設計、API仕様、データモデル、エラー処理、テスト仕様、デプロイメント仕様を定義しています。

この設計により、保守性が高く、拡張性に富んだシステムが実現できます。今後の開発では、この詳細設計書を基に実装を進め、必要に応じて更新していきます。