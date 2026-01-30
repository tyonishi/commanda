using System.Windows;
using Commanda.Core;

namespace Commanda;

/// <summary>
/// 実行履歴画面
/// </summary>
public partial class HistoryWindow : Window
{
    public HistoryWindow(IRepository<ExecutionLog> repository)
    {
        InitializeComponent();
        DataContext = new HistoryViewModel(repository);
    }
}
