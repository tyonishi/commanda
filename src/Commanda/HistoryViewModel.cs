using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Commanda.Core;

namespace Commanda;

/// <summary>
/// 実行履歴画面のViewModel
/// </summary>
public class HistoryViewModel : INotifyPropertyChanged
{
    private readonly IRepository<ExecutionLog> _repository;
    private ExecutionLog? _selectedLog;
    private string _searchText = string.Empty;
    private DateTime _startDate = DateTime.Now.AddDays(-7);
    private DateTime _endDate = DateTime.Now;
    private string _statusFilter = "All";
    private string _statusMessage = string.Empty;
    private ObservableCollection<ExecutionLog> _executionLogs = new();

    /// <summary>
    /// 実行履歴一覧
    /// </summary>
    public ObservableCollection<ExecutionLog> ExecutionLogs
    {
        get => _executionLogs;
        private set
        {
            if (_executionLogs != value)
            {
                _executionLogs = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredLogs));
                OnPropertyChanged(nameof(TotalLogsCount));
            }
        }
    }

    /// <summary>
    /// フィルタリングされた実行履歴
    /// </summary>
    public IEnumerable<ExecutionLog> FilteredLogs
    {
        get
        {
            var query = ExecutionLogs.AsEnumerable();

            // 検索テキストでフィルタリング
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchLower = SearchText.ToLower();
                query = query.Where(l =>
                    l.TaskDescription?.ToLower().Contains(searchLower) == true ||
                    l.Status?.ToLower().Contains(searchLower) == true ||
                    l.ErrorMessage?.ToLower().Contains(searchLower) == true);
            }

            // ステータスでフィルタリング
            if (StatusFilter != "All")
            {
                query = query.Where(l => l.Status == StatusFilter);
            }

            // 日付範囲でフィルタリング
            query = query.Where(l => l.Timestamp >= StartDate && l.Timestamp <= EndDate.AddDays(1));

            return query.OrderByDescending(l => l.Timestamp);
        }
    }

    /// <summary>
    /// 選択中のログ
    /// </summary>
    public ExecutionLog? SelectedLog
    {
        get => _selectedLog;
        set
        {
            if (_selectedLog != value)
            {
                _selectedLog = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 検索テキスト
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredLogs));
            }
        }
    }

    /// <summary>
    /// 開始日
    /// </summary>
    public DateTime StartDate
    {
        get => _startDate;
        set
        {
            if (_startDate != value)
            {
                _startDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredLogs));
            }
        }
    }

    /// <summary>
    /// 終了日
    /// </summary>
    public DateTime EndDate
    {
        get => _endDate;
        set
        {
            if (_endDate != value)
            {
                _endDate = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredLogs));
            }
        }
    }

    /// <summary>
    /// ステータスフィルター
    /// </summary>
    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (_statusFilter != value)
            {
                _statusFilter = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FilteredLogs));
            }
        }
    }

    /// <summary>
    /// ステータスメッセージ
    /// </summary>
    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage != value)
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 総ログ数
    /// </summary>
    public int TotalLogsCount => ExecutionLogs.Count;

    /// <summary>
    /// 利用可能なステータスフィルター
    /// </summary>
    public string[] AvailableStatusFilters { get; } = new[] { "All", "Success", "Failed", "Cancelled" };

    /// <summary>
    /// 履歴を更新するコマンド
    /// </summary>
    public ICommand RefreshCommand { get; }

    /// <summary>
    /// 履歴をクリアするコマンド
    /// </summary>
    public ICommand ClearHistoryCommand { get; }

    /// <summary>
    /// 履歴をエクスポートするコマンド
    /// </summary>
    public ICommand ExportHistoryCommand { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="repository">実行ログリポジトリ</param>
    public HistoryViewModel(IRepository<ExecutionLog> repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        RefreshCommand = new RelayCommand(() => _ = LoadHistoryAsync());
        ClearHistoryCommand = new RelayCommand(() => _ = ClearHistoryAsync());
        ExportHistoryCommand = new RelayCommand(() => _ = ExportHistoryAsync());

        // 初期データ読み込み
        _ = LoadHistoryAsync();
    }

    /// <summary>
    /// 実行履歴を読み込みます
    /// </summary>
    private async Task LoadHistoryAsync()
    {
        try
        {
            var logs = await _repository.GetAllAsync();
            var logList = logs?.ToList() ?? new List<ExecutionLog>();
            ExecutionLogs = new ObservableCollection<ExecutionLog>(logList);
            StatusMessage = $"{logList.Count}件の履歴を読み込みました";
        }
        catch (Exception ex)
        {
            StatusMessage = $"履歴読み込みエラー: {ex.Message}";
        }
    }

    /// <summary>
    /// 実行履歴をクリアします
    /// </summary>
    private async Task ClearHistoryAsync()
    {
        try
        {
            // すべてのログを削除
            var logs = await _repository.GetAllAsync();
            foreach (var log in logs)
            {
                await _repository.DeleteAsync(log);
            }

            ExecutionLogs.Clear();
            OnPropertyChanged(nameof(FilteredLogs));
            OnPropertyChanged(nameof(TotalLogsCount));
            StatusMessage = "履歴をクリアしました";
        }
        catch (Exception ex)
        {
            StatusMessage = $"履歴クリアエラー: {ex.Message}";
        }
    }

    /// <summary>
    /// 実行履歴をエクスポートします
    /// </summary>
    private async Task ExportHistoryAsync()
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = "csv",
                FileName = $"Commanda_History_{DateTime.Now:yyyyMMdd}"
            };

            if (dialog.ShowDialog() == true)
            {
                var logs = FilteredLogs.ToList();
                var extension = System.IO.Path.GetExtension(dialog.FileName).ToLower();

                if (extension == ".csv")
                {
                    await ExportToCsvAsync(dialog.FileName, logs);
                }
                else if (extension == ".json")
                {
                    await ExportToJsonAsync(dialog.FileName, logs);
                }

                StatusMessage = $"{logs.Count}件をエクスポートしました";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"エクスポートエラー: {ex.Message}";
        }
    }

    /// <summary>
    /// CSV形式でエクスポート
    /// </summary>
    private async Task ExportToCsvAsync(string filePath, List<ExecutionLog> logs)
    {
        var lines = new List<string>
        {
            "ID,Timestamp,TaskDescription,Status,Duration,StepsExecuted,ErrorMessage"
        };

        foreach (var log in logs)
        {
            var error = log.ErrorMessage?.Replace("\"", "\"\"") ?? "";
            lines.Add($"{log.Id},{log.Timestamp:yyyy-MM-dd HH:mm:ss},{log.TaskDescription},{log.Status},{log.Duration},{log.StepsExecuted},\"{error}\"");
        }

        await System.IO.File.WriteAllLinesAsync(filePath, lines);
    }

    /// <summary>
    /// JSON形式でエクスポート
    /// </summary>
    private async Task ExportToJsonAsync(string filePath, List<ExecutionLog> logs)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(logs, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        });

        await System.IO.File.WriteAllTextAsync(filePath, json);
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
