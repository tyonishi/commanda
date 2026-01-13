using Commanda.Core;
using Commanda.Extensions;

namespace Commanda.Mcp;

/// <summary>
/// MCPサーバーの実装
/// </summary>
public class McpServer : IMcpServer
{
    private readonly IExtensionManager _extensionManager;
    private bool _isInitialized;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="extensionManager">拡張機能マネージャー</param>
    public McpServer(IExtensionManager extensionManager)
    {
        _extensionManager = extensionManager ?? throw new ArgumentNullException(nameof(extensionManager));
    }

    /// <summary>
    /// MCPサーバーを初期化します
    /// </summary>
    /// <returns>初期化処理のタスク</returns>
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

    /// <summary>
    /// 指定されたツールを実行します
    /// </summary>
    /// <param name="toolName">ツール名</param>
    /// <param name="arguments">引数</param>
    /// <param name="timeout">タイムアウト時間</param>
    /// <returns>実行結果</returns>
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

    /// <summary>
    /// 利用可能なツールのリストを取得します
    /// </summary>
    /// <returns>ツール名のリスト</returns>
    public async Task<List<string>> GetAvailableToolsAsync()
    {
        if (!_isInitialized)
        {
            return new List<string>();
        }

        var tools = new List<string>();

        // 組み込みツールの追加
        tools.AddRange(new[] { "read_file", "write_file", "list_directory" });

        // 拡張ツールの追加（今回は空）
        var extensions = await _extensionManager.GetLoadedExtensionsAsync();
        foreach (var extension in extensions)
        {
            foreach (var toolType in extension.ToolTypes)
            {
                // 簡易的なツール名取得（実際の実装ではリフレクションを使う）
                tools.Add($"extension_{extension.Name}_{toolType.Name}");
            }
        }

        return tools;
    }

    /// <summary>
    /// 拡張機能を登録します
    /// </summary>
    /// <param name="extension">拡張機能</param>
    /// <returns>登録が成功したかどうか</returns>
    public async Task<bool> RegisterExtensionAsync(IMcpExtension extension)
    {
        return await _extensionManager.RegisterExtensionAsync(extension);
    }

    /// <summary>
    /// 拡張機能を登録解除します
    /// </summary>
    /// <param name="extensionName">拡張機能名</param>
    /// <returns>登録解除が成功したかどうか</returns>
    public async Task<bool> UnregisterExtensionAsync(string extensionName)
    {
        return await _extensionManager.UnregisterExtensionAsync(extensionName);
    }

    /// <summary>
    /// 内部ツール実行メソッド
    /// </summary>
    /// <param name="toolName">ツール名</param>
    /// <param name="arguments">引数</param>
    /// <param name="timeout">タイムアウト</param>
    /// <returns>実行結果</returns>
    private async Task<ToolResult> ExecuteToolInternalAsync(string toolName, Dictionary<string, object> arguments, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        var token = cts.Token;

        // 組み込みツールの実行
        switch (toolName)
        {
            case "read_file":
                return await FileOperations.ReadFileAsync(arguments, token);
            case "write_file":
                return await FileOperations.WriteFileAsync(arguments, token);
            case "list_directory":
                return await FileOperations.ListDirectoryAsync(arguments, token);
            default:
                // 拡張ツールの検索（今回は未実装）
                throw new ToolNotFoundException($"ツール '{toolName}' が見つかりません");
        }
    }
}