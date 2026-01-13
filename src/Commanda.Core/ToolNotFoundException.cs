namespace Commanda.Core;

/// <summary>
/// ツールが見つからない場合の例外
/// </summary>
public class ToolNotFoundException : CommandaException
{
    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="toolName">ツール名</param>
    public ToolNotFoundException(string toolName)
        : base($"ツール '{toolName}' が見つかりません", "TOOL_NOT_FOUND",
              new Dictionary<string, object> { ["toolName"] = toolName })
    {
    }
}