using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Commanda.Core;

namespace Commanda;

/// <summary>
/// メインウィンドウのビューモデル
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private readonly IAgentOrchestrator _agentOrchestrator;
    private string _currentMessage = string.Empty;
    private bool _isExecuting;

    /// <summary>
    /// メッセージのコレクション
    /// </summary>
    public ObservableCollection<MessageViewModel> Messages { get; } = new();

    /// <summary>
    /// 現在のメッセージ
    /// </summary>
    public string CurrentMessage
    {
        get => _currentMessage;
        set
        {
            if (_currentMessage != value)
            {
                _currentMessage = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 実行中かどうか
    /// </summary>
    public bool IsExecuting
    {
        get => _isExecuting;
        set
        {
            if (_isExecuting != value)
            {
                _isExecuting = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 送信コマンド
    /// </summary>
    public RelayCommand SendCommand { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="agentOrchestrator">エージェントオーケストレーター</param>
    public MainViewModel(IAgentOrchestrator agentOrchestrator)
    {
        _agentOrchestrator = agentOrchestrator ?? throw new ArgumentNullException(nameof(agentOrchestrator));

        SendCommand = new RelayCommand(
            async () => await SendMessageAsync(),
            () => !string.IsNullOrWhiteSpace(CurrentMessage) && !IsExecuting);
    }

    /// <summary>
    /// メッセージを送信します
    /// </summary>
    /// <returns>送信処理のタスク</returns>
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

    /// <summary>
    /// プロパティ変更イベント
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// プロパティ変更を通知します
    /// </summary>
    /// <param name="propertyName">プロパティ名</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}