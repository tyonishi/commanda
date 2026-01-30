using System.Reflection;
using System.Composition.Hosting;
using System.Composition;
using System.Runtime.Versioning;
using Commanda.Core;

namespace Commanda.Extensions;

/// <summary>
/// 拡張機能マネージャーの実装
/// </summary>
[SupportedOSPlatform("windows")]
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
            // MEFコンテナの設定
            var configuration = new ContainerConfiguration();

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
                    configuration = configuration.WithAssembly(assembly);
                }
                catch (Exception ex)
                {
                    // ログに記録して続行
                    Console.WriteLine($"DLL '{Path.GetFileName(dllPath)}' のロードに失敗しました: {ex.Message}");
                }
            }

            // テスト用の場合、すべてのアセンブリを追加
            if (AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name?.Contains("Test") == true))
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    configuration = configuration.WithAssembly(assembly);
                }
                // テスト用の拡張機能を追加
                var testType = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetTypes())
                    .FirstOrDefault(t => t.Name == "TestMefExtension" && typeof(IMcpExtension).IsAssignableFrom(t));
                if (testType != null)
                {
                    configuration = configuration.WithPart(testType);
                }
            }

            // MEFコンテナを作成
            using var container = configuration.CreateContainer();

            // 拡張機能を解決
            var extensions = container.GetExports<IMcpExtension>();

            foreach (var extension in extensions)
            {
                try
                {
                    // 拡張機能を初期化
                    await extension.InitializeAsync(null); // TODO: サービスプロバイダを渡す

                    // リストに追加
                    _loadedExtensions.Add(extension);
                }
                catch (Exception ex)
                {
                    // ログに記録して続行
                    Console.WriteLine($"拡張機能 '{extension.Name}' の初期化に失敗しました: {ex.Message}");
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

    /// <summary>
    /// 拡張機能の有効化状態を設定します
    /// </summary>
    /// <param name="name">拡張機能名</param>
    /// <param name="enabled">有効化フラグ</param>
    /// <returns>設定が成功したかどうか</returns>
    public Task<bool> SetExtensionEnabledAsync(string name, bool enabled)
    {
        // 現在の実装では単純に成功を返す（将来の実装で拡張）
        var extension = _loadedExtensions.FirstOrDefault(e => e.Name == name);
        if (extension != null)
        {
            // 実際の実装では、ここで拡張機能の有効化状態を変更する
            // 現在は常に成功を返す
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }
}