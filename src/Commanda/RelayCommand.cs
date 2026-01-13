using System.Windows.Input;

namespace Commanda;

/// <summary>
/// リレーコマンドの実装
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="execute">実行するアクション</param>
    public RelayCommand(Action execute)
        : this(execute, null)
    {
    }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="execute">実行するアクション</param>
    /// <param name="canExecute">実行可能かどうかを判定する関数</param>
    public RelayCommand(Action execute, Func<bool>? canExecute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// 実行可能かどうかが変更されたときに発生します
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>
    /// コマンドが実行可能かどうかを判定します
    /// </summary>
    /// <param name="parameter">パラメータ</param>
    /// <returns>実行可能かどうか</returns>
    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute();
    }

    /// <summary>
    /// コマンドを実行します
    /// </summary>
    /// <param name="parameter">パラメータ</param>
    public void Execute(object? parameter)
    {
        _execute();
    }
}

/// <summary>
/// ジェネリック版リレーコマンド
/// </summary>
public class RelayCommand<T> : ICommand
{
    private readonly Action<T> _execute;
    private readonly Predicate<T>? _canExecute;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="execute">実行するアクション</param>
    public RelayCommand(Action<T> execute)
        : this(execute, null)
    {
    }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="execute">実行するアクション</param>
    /// <param name="canExecute">実行可能かどうかを判定する関数</param>
    public RelayCommand(Action<T> execute, Predicate<T>? canExecute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// 実行可能かどうかが変更されたときに発生します
    /// </summary>
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <summary>
    /// コマンドが実行可能かどうかを判定します
    /// </summary>
    /// <param name="parameter">パラメータ</param>
    /// <returns>実行可能かどうか</returns>
    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute((T)parameter!);
    }

    /// <summary>
    /// コマンドを実行します
    /// </summary>
    /// <param name="parameter">パラメータ</param>
    public void Execute(object? parameter)
    {
        _execute((T)parameter!);
    }
}