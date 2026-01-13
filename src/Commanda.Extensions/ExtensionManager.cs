using Commanda.Core;

namespace Commanda.Extensions;

/// <summary>
/// 拡張機能マネージャーの実装
/// </summary>
public class ExtensionManager : IExtensionManager
{
    private readonly List<IMcpExtension> _loadedExtensions = new();

    /// <summary>
    /// 拡張機能をロードします
    /// </summary>
    /// <returns>ロード処理のタスク</returns>
    public Task LoadExtensionsAsync()
    {
        // 今回は何もロードしない（将来の拡張用）
        return Task.CompletedTask;
    }

    /// <summary>
    /// ロードされた拡張機能を取得します
    /// </summary>
    /// <returns>拡張機能のリスト</returns>
    public Task<IEnumerable<IMcpExtension>> GetLoadedExtensionsAsync()
    {
        return Task.FromResult<IEnumerable<IMcpExtension>>(_loadedExtensions);
    }

    /// <summary>
    /// 拡張機能を登録します
    /// </summary>
    /// <param name="extension">拡張機能</param>
    /// <returns>登録が成功したかどうか</returns>
    public Task<bool> RegisterExtensionAsync(IMcpExtension extension)
    {
        if (!_loadedExtensions.Contains(extension))
        {
            _loadedExtensions.Add(extension);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    /// <summary>
    /// 拡張機能を登録解除します
    /// </summary>
    /// <param name="extensionName">拡張機能名</param>
    /// <returns>登録解除が成功したかどうか</returns>
    public Task<bool> UnregisterExtensionAsync(string extensionName)
    {
        var extension = _loadedExtensions.FirstOrDefault(e => e.Name == extensionName);
        if (extension != null)
        {
            _loadedExtensions.Remove(extension);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    /// <summary>
    /// 拡張機能をリロードします
    /// </summary>
    /// <returns>リロード処理のタスク</returns>
    public async Task ReloadExtensionsAsync()
    {
        _loadedExtensions.Clear();
        await LoadExtensionsAsync();
    }
}