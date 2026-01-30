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
        tools.AddRange(new[] { 
            "read_file", "write_file", "list_directory",
            "launch_application", "close_application", "get_running_applications",
            "read_text_file", "write_text_file", "append_to_file", "search_in_file", "replace_in_file"
        });

        // 拡張ツールの追加
        var extensions = await _extensionManager.GetLoadedExtensionsAsync();
        foreach (var extension in extensions)
        {
            foreach (var toolType in extension.ToolTypes)
            {
                // ツール名を生成（拡張機能名_ツールクラス名）
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
    /// 拡張ツールを実行します
    /// </summary>
    /// <param name="toolName">ツール名</param>
    /// <param name="arguments">引数</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>実行結果</returns>
    private async Task<ToolResult?> ExecuteExtensionToolAsync(string toolName, Dictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        // ツール名から拡張機能名とツールクラス名を解析
        if (!toolName.StartsWith("extension_"))
        {
            return null;
        }

        var parts = toolName.Split('_');
        if (parts.Length < 3)
        {
            return null;
        }

        var extensionName = parts[1];
        var toolClassName = string.Join("_", parts.Skip(2));

        // 拡張機能を取得
        var extensions = await _extensionManager.GetLoadedExtensionsAsync();
        var extension = extensions.FirstOrDefault(e => e.Name == extensionName);
        if (extension == null)
        {
            return null;
        }

        // ツールクラスを取得
        var toolType = extension.ToolTypes.FirstOrDefault(t => t.Name == toolClassName);
        if (toolType == null)
        {
            return null;
        }

        // ツールクラスのインスタンスを作成
        var toolInstance = Activator.CreateInstance(toolType);
        if (toolInstance == null)
        {
            return null;
        }

        try
        {
            // ツールのメソッドをリフレクションで実行
            // TODO: MCPツール属性に基づいて適切なメソッドを呼び出す
            // ここでは簡易実装として、ExecuteAsyncメソッドを呼び出す
            var executeMethod = toolType.GetMethod("ExecuteAsync");
            if (executeMethod != null)
            {
                var result = await (Task<ToolResult>)executeMethod.Invoke(toolInstance, new object[] { arguments, cancellationToken })!;
                return result;
            }

            return null;
        }
        catch (Exception ex)
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = $"拡張ツール実行エラー: {ex.Message}"
            };
        }
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
            // ApplicationControlツール
            case "launch_application":
                return await ApplicationControl.LaunchApplicationAsync(arguments, token);
            case "close_application":
                return await ApplicationControl.CloseApplicationAsync(arguments, token);
            case "get_running_applications":
                return await ApplicationControl.GetRunningApplicationsAsync(arguments, token);
            // TextProcessingツール
            case "read_text_file":
                return await TextProcessing.ReadTextFileAsync(arguments, token);
            case "write_text_file":
                return await TextProcessing.WriteTextFileAsync(arguments, token);
            case "append_to_file":
                return await TextProcessing.AppendToFileAsync(arguments, token);
            case "search_in_file":
                return await TextProcessing.SearchInFileAsync(arguments, token);
            case "replace_in_file":
                return await TextProcessing.ReplaceInFileAsync(arguments, token);
            default:
                // 拡張ツールの実行を試行
                var result = await ExecuteExtensionToolAsync(toolName, arguments, token);
                if (result != null)
                {
                    return result;
                }

                throw new ToolNotFoundException($"ツール '{toolName}' が見つかりません");
        }
    }
}
