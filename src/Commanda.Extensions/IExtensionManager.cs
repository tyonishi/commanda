using Commanda.Core;

namespace Commanda.Extensions;

/// <summary>
/// 拡張機能マネージャーのインターフェース
/// </summary>
public interface IExtensionManager
{
    /// <summary>
    /// 拡張機能をロードします
    /// </summary>
    /// <returns>ロード処理のタスク</returns>
    Task LoadExtensionsAsync();

    /// <summary>
    /// ロードされた拡張機能を取得します
    /// </summary>
    /// <returns>拡張機能のリスト</returns>
    Task<IEnumerable<IMcpExtension>> GetLoadedExtensionsAsync();

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

    /// <summary>
    /// 拡張機能をリロードします
    /// </summary>
    /// <returns>リロード処理のタスク</returns>
    Task ReloadExtensionsAsync();
}

/// <summary>
/// MCPツールタイプの属性
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class McpServerToolTypeAttribute : Attribute
{
}

/// <summary>
/// MCPサーバーツールの属性
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class McpServerToolAttribute : Attribute
{
    /// <summary>
    /// ツール名
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 説明
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="name">ツール名</param>
    /// <param name="description">説明</param>
    public McpServerToolAttribute(string name, string description)
    {
        Name = name;
        Description = description;
    }
}