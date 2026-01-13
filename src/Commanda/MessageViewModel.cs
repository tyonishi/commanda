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