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
<!-- MainWindow.xaml -->
<Window x:Class="Commanda.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:local="clr-namespace:Commanda"
        xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
        mc:Ignorable="d"
        Title="Commanda - AI PC Automation" Height="600" Width="800"
        WindowStartupLocation="CenterScreen"
        TextElement.FontWeight="Regular"
        TextElement.FontSize="13"
        TextOptions.TextFormattingMode="Ideal"
        TextOptions.TextRenderingMode="Auto">
    <Window.Resources>
        <BooleanToVisibilityConverter x:Key="BooleanToVisibilityConverter"/>
        <local:InitialToColorConverter x:Key="InitialToColorConverter"/>
    </Window.Resources>

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- メッセージリスト -->
        <ScrollViewer Grid.Row="0" VerticalScrollBarVisibility="Auto" Margin="16">
            <ItemsControl ItemsSource="{Binding Messages}" Margin="0,0,0,16">
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <materialDesign:Card Margin="0,8" Padding="12" MaxWidth="600"
                                              Background="{DynamicResource MaterialDesignPaper}"
                                              UniformCornerRadius="8">
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
                                    <Grid>
                                        <Grid.ColumnDefinitions>
                                            <ColumnDefinition Width="*"/>
                                            <ColumnDefinition Width="Auto"/>
                                        </Grid.ColumnDefinitions>

                                        <TextBlock Grid.Column="0" Text="{Binding SenderName}"
                                                   FontSize="16" FontWeight="SemiBold"
                                                   Margin="0,0,0,4"/>

                                        <!-- ステータスアイコン -->
                                        <TextBlock Grid.Column="1" Text="✓" Foreground="Green" Margin="8,0,0,0"
                                                   Visibility="{Binding IsSuccess, Converter={StaticResource BooleanToVisibilityConverter}}"/>
                                        <TextBlock Grid.Column="1" Text="⚠" Foreground="Red" Margin="8,0,0,0"
                                                   Visibility="{Binding IsSuccess, Converter={StaticResource BooleanToVisibilityConverter}, ConverterParameter=inverse}"/>
                                    </Grid>

                                    <local:MarkdownTextBlock MarkdownText="{Binding Content}"
                                                             FontSize="14" LineHeight="1.4"
                                                             Margin="0,0,0,8"/>

                                    <!-- 実行詳細 -->
                                    <StackPanel Orientation="Horizontal" Margin="0,4,0,0"
                                                Visibility="{Binding IsSystemMessage, Converter={StaticResource BooleanToVisibilityConverter}, ConverterParameter=inverse}">
                                        <TextBlock Text="{Binding Timestamp, StringFormat='{}{0:HH:mm}'}"
                                                   FontSize="11" Foreground="#666666"/>
                                        <TextBlock Text=" • " FontSize="11" Foreground="#666666"
                                                   Visibility="{Binding Duration, Converter={StaticResource BooleanToVisibilityConverter}}"/>
                                        <TextBlock Text="{Binding Duration, StringFormat='{}{0:mm\\:ss\\.fff}'}"
                                                   FontSize="11" Foreground="#666666"
                                                   Visibility="{Binding Duration, Converter={StaticResource BooleanToVisibilityConverter}}"/>
                                    </StackPanel>

                                    <!-- 警告メッセージ -->
                                    <ItemsControl ItemsSource="{Binding Warnings}" Margin="0,8,0,0">
                                        <ItemsControl.ItemTemplate>
                                            <DataTemplate>
                                                <Border Background="Orange" CornerRadius="12" Padding="8,4"
                                                        Margin="0,0,8,4">
                                                    <TextBlock Text="{Binding}" Foreground="White" FontSize="11"/>
                                                </Border>
                                            </DataTemplate>
                                        </ItemsControl.ItemTemplate>
                                        <ItemsControl.ItemsPanel>
                                            <ItemsPanelTemplate>
                                                <WrapPanel/>
                                            </ItemsPanelTemplate>
                                        </ItemsControl.ItemsPanel>
                                    </ItemsControl>
                                </StackPanel>
                            </Grid>
                        </materialDesign:Card>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </ScrollViewer>

        <!-- 入力エリア -->
        <materialDesign:Card Grid.Row="1" Margin="16,0,16,16" Padding="16"
                             Background="{DynamicResource MaterialDesignCardBackground}">
            <Grid>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>

                <!-- テキスト入力 -->
                <TextBox Grid.Column="0" Text="{Binding CurrentMessage, UpdateSourceTrigger=PropertyChanged}"
                         AcceptsReturn="True" TextWrapping="Wrap" VerticalScrollBarVisibility="Auto"
                         MinHeight="48" Margin="0,0,12,0" Padding="8"
                         IsEnabled="{Binding IsExecuting, Converter={StaticResource BooleanToVisibilityConverter}, ConverterParameter=inverse}"/>

                <!-- 送信ボタン -->
                <Button Grid.Column="1" Content="送信"
                        Command="{Binding SendCommand}"
                        Width="80" Height="48"
                        ToolTip="送信"
                        IsEnabled="{Binding IsExecuting, Converter={StaticResource BooleanToVisibilityConverter}, ConverterParameter=inverse}"/>

                <!-- 実行中インジケーター -->
                <StackPanel Grid.Column="0" Grid.ColumnSpan="2"
                           HorizontalAlignment="Center" VerticalAlignment="Center"
                           Visibility="{Binding IsExecuting, Converter={StaticResource BooleanToVisibilityConverter}}">
                    <TextBlock Text="⏳" FontSize="24" Margin="0,0,0,8"/>
                    <TextBlock Text="実行中..." FontSize="12" Foreground="#666666"/>
                </StackPanel>
            </Grid>
        </materialDesign:Card>
    </Grid>
</Window>
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
    private readonly ITaskPlanner _taskPlanner;
    private readonly IExecutionMonitor _executionMonitor;
    private readonly IStateManager _stateManager;
    private readonly ILlmProviderManager _llmManager;
    private readonly IMcpServer _mcpServer;
    private readonly InputValidator _inputValidator;
    private readonly ILogger<AgentOrchestrator> _logger;
    private readonly CancellationTokenSource _cancellationSource;
    private AgentContext? _currentContext;

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

#### 1.4.1 実装済みツール一覧

**Phase 1で実装されたツール:**

| ツール名 | カテゴリ | 機能 | パラメータ |
|---------|---------|------|-----------|
| `read_file` | FileOperations | ファイル読み込み | path |
| `write_file` | FileOperations | ファイル書き込み | path, content |
| `list_directory` | FileOperations | ディレクトリ一覧 | path |
| `launch_application` | ApplicationControl | アプリ起動 | path, arguments, working_directory |
| `close_application` | ApplicationControl | アプリ終了 | process_id |
| `get_running_applications` | ApplicationControl | 実行中アプリ一覧 | - |
| `read_text_file` | TextProcessing | テキスト読み込み | path, encoding |
| `write_text_file` | TextProcessing | テキスト書き込み | path, content, encoding, create_backup |
| `append_to_file` | TextProcessing | ファイル追記 | path, content, encoding |
| `search_in_file` | TextProcessing | ファイル内検索 | path, pattern, use_regex |
| `replace_in_file` | TextProcessing | ファイル内置換 | path, old_text, new_text, use_regex, create_backup |

**セキュリティ機能:**
- 危険なコマンドパターンブロック（format, del, regedit等）
- システムパス書き込み禁止（Windows/System32等）
- ファイルサイズ制限（10MB）
- システムプロセス保護

#### 1.4.2 対応LLMプロバイダー（Phase 3実装済み）

| プロバイダー | クラス名 | APIタイプ | ストリーミング | 認証 |
|------------|---------|----------|--------------|------|
| **OpenAI** | `OpenAiProvider` | OpenAI API | ✅ SSE | APIキー |
| **Anthropic** | `AnthropicProvider` | Claude Messages API | ✅ SSE | APIキー |
| **Ollama** | `OllamaProvider` | Ollama Generate API | ✅ NDJSON | 不要 |
| **LM Studio** | `LmStudioProvider` | OpenAI互換 API | ✅ SSE | 不要 |

#### 1.4.3 インターフェース定義
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
    public object? Output { get; set; }
    public string? Error { get; set; }
    public TimeSpan Duration { get; set; }
}
```

#### 1.4.2 実装クラス
```csharp
public class McpServer : IMcpServer
{
    private readonly IExtensionManager _extensionManager;
    private bool _isInitialized;

    public McpServer(IExtensionManager extensionManager)
    {
        _extensionManager = extensionManager ?? throw new ArgumentNullException(nameof(extensionManager));
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        // 拡張機能をロード
        await _extensionManager.LoadExtensionsAsync();

        _isInitialized = true;
    }
    
    public async Task<ToolResult> ExecuteToolAsync(string toolName, Dictionary<string, object> arguments, TimeSpan timeout)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("MCPサーバーが初期化されていません");
        }

        var startTime = DateTime.UtcNow;

        try
        {
            var result = await ExecuteToolInternalAsync(toolName, arguments, timeout);

            var duration = DateTime.UtcNow - startTime;
            result.Duration = duration;

            return result;
        }
        catch (Exception ex)
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = ex.Message,
                Duration = DateTime.UtcNow - startTime
            };
        }
    }

    private async Task<ToolResult> ExecuteToolInternalAsync(
        string toolName,
        Dictionary<string, object> arguments,
        TimeSpan timeout)
    {
        // 拡張ツールの検索
        var extensions = await _extensionManager.GetLoadedExtensionsAsync();
        foreach (var extension in extensions)
        {
            var toolType = extension.ToolTypes.FirstOrDefault(t =>
                GetToolNameFromType(t) == toolName);

            if (toolType != null)
            {
                return await ExecuteExtensionToolAsync(toolType, arguments);
            }
        }

        // 組み込みツールの実行
        return await ExecuteBuiltInToolAsync(toolName, arguments);
    }

    private async Task<ToolResult> ExecuteExtensionToolAsync(
        McpServerToolType toolType,
        Dictionary<string, object> arguments)
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
                var result = await (Task)method.Invoke(instance, new object[] { arguments });
                return new ToolResult { IsSuccessful = true, Output = result };
            }
        }

        throw new ToolNotFoundException($"ツール '{toolName}' が見つかりません");
    }

    private async Task<ToolResult> ExecuteBuiltInToolAsync(
        string toolName,
        Dictionary<string, object> arguments)
    {
        // 組み込みツールの実装（例: ファイル操作）
        switch (toolName)
        {
            case "read_file":
                return await FileOperations.ReadFileAsync(arguments);
            case "write_file":
                return await FileOperations.WriteFileAsync(arguments);
            case "list_directory":
                return await FileOperations.ListDirectoryAsync(arguments);
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
public class AgentOrchestratorTests
{
    private readonly Mock<ITaskPlanner> _taskPlannerMock;
    private readonly Mock<IExecutionMonitor> _executionMonitorMock;
    private readonly Mock<IStateManager> _stateManagerMock;
    private readonly Mock<ILlmProviderManager> _llmManagerMock;
    private readonly Mock<IMcpServer> _mcpServerMock;
    private readonly InputValidator _inputValidator;
    private readonly Mock<ILogger<AgentOrchestrator>> _loggerMock;
    private readonly AgentOrchestrator _orchestrator;

    public AgentOrchestratorTests()
    {
        _taskPlannerMock = new Mock<ITaskPlanner>();
        _executionMonitorMock = new Mock<IExecutionMonitor>();
        _stateManagerMock = new Mock<IStateManager>();
        _llmManagerMock = new Mock<ILlmProviderManager>();
        _mcpServerMock = new Mock<IMcpServer>();
        _inputValidator = new InputValidator();
        _loggerMock = new Mock<ILogger<AgentOrchestrator>>();

        // Setup default behaviors to prevent null reference exceptions
        _executionMonitorMock.Setup(m => m.EvaluateResultAsync(It.IsAny<ExecutionResult>(), It.IsAny<AgentContext>()))
                             .ReturnsAsync(new EvaluationResult { IsSuccessful = false, ShouldRetry = false });
        _stateManagerMock.Setup(m => m.SaveStateAsync(It.IsAny<AgentContext>()))
                         .Returns(Task.CompletedTask);
        _mcpServerMock.Setup(m => m.ExecuteToolAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<TimeSpan>()))
                      .ReturnsAsync(new ToolResult { IsSuccessful = true, Output = "Mock output" });

        _orchestrator = new AgentOrchestrator(
            _taskPlannerMock.Object,
            _executionMonitorMock.Object,
            _stateManagerMock.Object,
            _llmManagerMock.Object,
            _mcpServerMock.Object,
            _inputValidator,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ExecuteTaskAsync_ValidInput_ReturnsSuccessfulResult()
    {
        // Arrange
        var userInput = "Hello, create a test file";
        var expectedContent = "Task completed successfully";

        // Setup task planner to return a simple plan
        var plan = new ExecutionPlan
        {
            Description = "Test plan",
            Steps = new List<ExecutionStep>
            {
                new ExecutionStep
                {
                    ToolName = "test_tool",
                    Arguments = new Dictionary<string, object> { { "input", "test" } },
                    ExpectedOutcome = "File created",
                    Timeout = TimeSpan.FromSeconds(30)
                }
            }
        };

        _taskPlannerMock.Setup(p => p.GeneratePlanAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(plan);

        // Setup MCP server to return successful tool result
        var toolResult = new ToolResult
        {
            IsSuccessful = true,
            Output = expectedContent,
            Duration = TimeSpan.FromSeconds(1)
        };

        _mcpServerMock.Setup(m => m.ExecuteToolAsync("test_tool", It.IsAny<Dictionary<string, object>>(), It.IsAny<TimeSpan>()))
                     .ReturnsAsync(toolResult);

        // Setup execution monitor to return successful evaluation
        var evaluationResult = new EvaluationResult
        {
            IsSuccessful = true,
            ShouldRetry = false,
            Feedback = "Task completed successfully"
        };

        _executionMonitorMock.Setup(m => m.EvaluateResultAsync(It.IsAny<ExecutionResult>(), It.IsAny<AgentContext>()))
                            .ReturnsAsync(evaluationResult);

        // Act
        var result = await _orchestrator.ExecuteTaskAsync(userInput);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Contains("正常に完了", result.Content);
        Assert.Equal(1, result.StepsExecuted);
        Assert.True(result.Duration > TimeSpan.Zero);

        // Verify interactions
        _taskPlannerMock.Verify(p => p.GeneratePlanAsync(It.IsAny<AgentContext>(), It.IsAny<CancellationToken>()), Times.Once);
        _mcpServerMock.Verify(m => m.ExecuteToolAsync("test_tool", It.IsAny<Dictionary<string, object>>(), It.IsAny<TimeSpan>()), Times.Once);
        _executionMonitorMock.Verify(m => m.EvaluateResultAsync(It.IsAny<ExecutionResult>(), It.IsAny<AgentContext>()), Times.Once);
        _stateManagerMock.Verify(m => m.SaveStateAsync(It.IsAny<AgentContext>()), Times.AtLeastOnce);
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