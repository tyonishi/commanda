namespace Commanda.Core;

/// <summary>
/// MCPサーバーのインターフェース
/// </summary>
public interface IMcpServer
{
    /// <summary>
    /// MCPサーバーを初期化します
    /// </summary>
    /// <returns>初期化処理のタスク</returns>
    Task InitializeAsync();

    /// <summary>
    /// 指定されたツールを実行します
    /// </summary>
    /// <param name="toolName">ツール名</param>
    /// <param name="arguments">引数</param>
    /// <param name="timeout">タイムアウト時間</param>
    /// <returns>実行結果</returns>
    Task<ToolResult> ExecuteToolAsync(string toolName, Dictionary<string, object> arguments, TimeSpan timeout);

    /// <summary>
    /// 利用可能なツールのリストを取得します
    /// </summary>
    /// <returns>ツール名のリスト</returns>
    Task<List<string>> GetAvailableToolsAsync();

    /// <summary>
    /// 拡張機能を登録します
    /// </summary>
    /// <param name="extension">拡張機能</param>
    /// <returns>登録が成功したかどうか</returns>
    Task<bool> RegisterExtensionAsync(IMcpExtension extension);

    /// <summary>
    /// 拡張機能を登録解除します
    /// </summary>
    /// <param name="extensionName">拡張機能名</param>
    /// <returns>登録解除が成功したかどうか</returns>
    Task<bool> UnregisterExtensionAsync(string extensionName);
}

/// <summary>
/// ツール実行結果を表すクラス
/// </summary>
public class ToolResult
{
    /// <summary>
    /// 実行が成功したかどうか
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// 実行結果の出力
    /// </summary>
    public object? Output { get; set; }

    /// <summary>
    /// エラーメッセージ
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// 実行にかかった時間
    /// </summary>
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// MCP拡張機能のインターフェース
/// </summary>
public interface IMcpExtension
{
    /// <summary>
    /// 拡張機能の名前
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 拡張機能のバージョン
    /// </summary>
    string Version { get; }

    /// <summary>
    /// 提供するツールタイプのリスト
    /// </summary>
    IEnumerable<Type> ToolTypes { get; }

    /// <summary>
    /// 拡張機能を初期化します
    /// </summary>
    /// <param name="services">サービスプロバイダ</param>
    /// <returns>初期化処理のタスク</returns>
    Task InitializeAsync(IServiceProvider? services);
}
