using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Commanda;

/// <summary>
/// メッセージのビューモデル
/// </summary>
public class MessageViewModel : INotifyPropertyChanged
{
    private string _senderName = string.Empty;
    private string _senderInitial = string.Empty;
    private string _content = string.Empty;
    private DateTime _timestamp;
    private bool _isSuccess = true;
    private TimeSpan _duration;
    private List<string> _warnings = new();
    private bool _isSystemMessage;

    /// <summary>
    /// 送信者名
    /// </summary>
    public string SenderName
    {
        get => _senderName;
        set
        {
            if (_senderName != value)
            {
                _senderName = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 送信者のイニシャル
    /// </summary>
    public string SenderInitial
    {
        get => _senderInitial;
        set
        {
            if (_senderInitial != value)
            {
                _senderInitial = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// メッセージ内容
    /// </summary>
    public string Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                _content = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// タイムスタンプ
    /// </summary>
    public DateTime Timestamp
    {
        get => _timestamp;
        set
        {
            if (_timestamp != value)
            {
                _timestamp = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 実行が成功したかどうか
    /// </summary>
    public bool IsSuccess
    {
        get => _isSuccess;
        set
        {
            if (_isSuccess != value)
            {
                _isSuccess = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 実行にかかった時間
    /// </summary>
    public TimeSpan Duration
    {
        get => _duration;
        set
        {
            if (_duration != value)
            {
                _duration = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 警告メッセージのリスト
    /// </summary>
    public List<string> Warnings
    {
        get => _warnings;
        set
        {
            if (_warnings != value)
            {
                _warnings = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// システムメッセージかどうか
    /// </summary>
    public bool IsSystemMessage
    {
        get => _isSystemMessage;
        set
        {
            if (_isSystemMessage != value)
            {
                _isSystemMessage = value;
                OnPropertyChanged();
            }
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
