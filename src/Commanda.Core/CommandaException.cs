namespace Commanda.Core;

/// <summary>
/// Commandaの基本例外クラス
/// </summary>
public class CommandaException : Exception
{
    /// <summary>
    /// エラーコード
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// コンテキスト情報
    /// </summary>
    public Dictionary<string, object> Context { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="message">メッセージ</param>
    public CommandaException(string message)
        : this(message, "GENERAL_ERROR", new Dictionary<string, object>())
    {
    }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="message">メッセージ</param>
    /// <param name="errorCode">エラーコード</param>
    public CommandaException(string message, string errorCode)
        : this(message, errorCode, new Dictionary<string, object>())
    {
    }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="message">メッセージ</param>
    /// <param name="errorCode">エラーコード</param>
    /// <param name="context">コンテキスト</param>
    public CommandaException(string message, string errorCode, Dictionary<string, object> context)
        : base(message)
    {
        ErrorCode = errorCode;
        Context = context ?? new Dictionary<string, object>();
    }
}