using System.Reflection;
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
    public async Task LoadExtensionsAsync()
    {
        try
        {
            // 拡張機能ディレクトリのパスを取得
            var extensionsPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Extensions");

            if (!Directory.Exists(extensionsPath))
            {
                Directory.CreateDirectory(extensionsPath);
            }

            // 拡張機能ディレクトリからDLLファイルを検索
            var extensionFiles = Directory.GetFiles(extensionsPath, "*.dll");

            foreach (var dllPath in extensionFiles)
            {
                try
                {
                    // アセンブリをロード
                    var assembly = Assembly.LoadFrom(dllPath);

                    // IMcpExtensionを実装したクラスを検索
                    var extensionTypes = assembly.GetTypes()
                        .Where(t => typeof(IMcpExtension).IsAssignableFrom(t) &&
                                   !t.IsInterface &&
                                   !t.IsAbstract);

                    foreach (var extensionType in extensionTypes)
                    {
                        try
                        {
                            // 拡張機能のインスタンスを作成
                            var instance = Activator.CreateInstance(extensionType);
                            if (instance is IMcpExtension extension)
                            {
                                // 拡張機能を初期化
                                await extension.InitializeAsync(null); // TODO: サービスプロバイダを渡す

                                // リストに追加
                                _loadedExtensions.Add(extension);
                            }
                        }
                        catch (Exception ex)
                        {
                            // ログに記録して続行
                            Console.WriteLine($"拡張機能 '{extensionType.Name}' の初期化に失敗しました: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // ログに記録して続行
                    Console.WriteLine($"DLL '{Path.GetFileName(dllPath)}' のロードに失敗しました: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            // ログに記録
            Console.WriteLine($"拡張機能のロードに失敗しました: {ex.Message}");
        }
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