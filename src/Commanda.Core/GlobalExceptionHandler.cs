using System.Reactive.Subjects;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using Commanda.Core;

namespace Commanda.Core;

/// <summary>
/// グローバル例外ハンドラーのインターフェース
/// </summary>
public interface IGlobalExceptionHandler
{
    /// <summary>
    /// 例外を処理します
    /// </summary>
    /// <param name="exception">例外</param>
    void HandleException(Exception exception);
}

/// <summary>
/// グローバル例外ハンドラー
/// </summary>
public class GlobalExceptionHandler : IGlobalExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="logger">ロガー</param>
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 例外を処理します
    /// </summary>
    /// <param name="exception">例外</param>
    public void HandleException(Exception exception)
    {
        _logger.LogError(exception, "未処理の例外が発生しました");

        // CommandaExceptionの場合はユーザー向けメッセージを表示
        if (exception is CommandaException commandaEx)
        {
            ShowUserFriendlyError(commandaEx);
        }
        else
        {
            ShowGenericError(exception);
        }
    }

    /// <summary>
    /// ユーザー向けエラーメッセージを表示します
    /// </summary>
    /// <param name="exception">CommandaException</param>
    private void ShowUserFriendlyError(CommandaException exception)
    {
        var message = GetUserFriendlyMessage(exception.ErrorCode);

        // WPFアプリケーションの場合、UIスレッドでメッセージを表示
        if (System.Windows.Application.Current != null)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                System.Windows.MessageBox.Show(
                    message,
                    "エラー",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            });
        }
        else
        {
            // コンソールアプリケーションの場合、コンソールに出力
            Console.WriteLine($"エラー: {message}");
        }
    }

    /// <summary>
    /// 一般的なエラーメッセージを表示します
    /// </summary>
    /// <param name="exception">例外</param>
    private void ShowGenericError(Exception exception)
    {
        var message = "予期しないエラーが発生しました。詳細はログを確認してください。";

        if (System.Windows.Application.Current != null)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                System.Windows.MessageBox.Show(
                    message,
                    "エラー",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            });
        }
        else
        {
            Console.WriteLine($"エラー: {message}");
        }
    }

    /// <summary>
    /// エラーコードからユーザー向けメッセージを取得します
    /// </summary>
    /// <param name="errorCode">エラーコード</param>
    /// <returns>ユーザー向けメッセージ</returns>
    private string GetUserFriendlyMessage(string errorCode)
    {
        return errorCode switch
        {
            "TOOL_NOT_FOUND" => "指定された操作が見つかりません。拡張機能が正しくインストールされているか確認してください。",
            "LLM_PROVIDER_ERROR" => "AIプロバイダとの通信に失敗しました。ネットワーク接続とAPIキーを確認してください。",
            "PLANNING_ERROR" => "タスクの計画作成に失敗しました。入力をより具体的にしてください。",
            "VALIDATION_ERROR" => "入力データの検証に失敗しました。入力内容を確認してください。",
            "SECURITY_ERROR" => "セキュリティ関連のエラーが発生しました。操作を中止します。",
            _ => "予期しないエラーが発生しました。アプリケーションを再起動してください。"
        };
    }
}

/// <summary>
/// Reactive Extensionsを使用した例外監視
/// </summary>
public class ExceptionMonitor : IObserver<Exception>, IDisposable
{
    private readonly IGlobalExceptionHandler _handler;
    private readonly IDisposable _subscription;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="handler">グローバル例外ハンドラー</param>
    /// <param name="exceptionSource">例外ソース</param>
    public ExceptionMonitor(IGlobalExceptionHandler handler, IObservable<Exception> exceptionSource)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _subscription = exceptionSource.Subscribe(this);
    }

    /// <summary>
    /// 例外を受信したときの処理
    /// </summary>
    /// <param name="exception">例外</param>
    public void OnNext(Exception exception)
    {
        _handler.HandleException(exception);
    }

    /// <summary>
    /// エラーが発生したときの処理
    /// </summary>
    /// <param name="error">エラー</param>
    public void OnError(Exception error)
    {
        _handler.HandleException(error);
    }

    /// <summary>
    /// 完了したときの処理
    /// </summary>
    public void OnCompleted()
    {
        // 何もしない
    }

    /// <summary>
    /// リソースを解放します
    /// </summary>
    public void Dispose()
    {
        _subscription?.Dispose();
    }
}