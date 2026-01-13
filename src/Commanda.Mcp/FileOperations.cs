using Commanda.Core;

namespace Commanda.Mcp;

/// <summary>
/// ファイル操作ツールの実装
/// </summary>
public static class FileOperations
{
    /// <summary>
    /// ファイルを読み込みます
    /// </summary>
    /// <param name="arguments">引数</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>実行結果</returns>
    public static async Task<ToolResult> ReadFileAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryGetValue("path", out var pathObj) || pathObj is not string path)
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = "pathパラメータが必要です"
            };
        }

        try
        {
            var content = await File.ReadAllTextAsync(path, cancellationToken);
            return new ToolResult
            {
                IsSuccessful = true,
                Output = content
            };
        }
        catch (Exception ex)
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = $"ファイル読み込みエラー: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// ファイルに書き込みます
    /// </summary>
    /// <param name="arguments">引数</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>実行結果</returns>
    public static async Task<ToolResult> WriteFileAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryGetValue("path", out var pathObj) || pathObj is not string path)
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = "pathパラメータが必要です"
            };
        }

        if (!arguments.TryGetValue("content", out var contentObj) || contentObj is not string content)
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = "contentパラメータが必要です"
            };
        }

        try
        {
            await File.WriteAllTextAsync(path, content, cancellationToken);
            return new ToolResult
            {
                IsSuccessful = true,
                Output = "ファイルが正常に書き込まれました"
            };
        }
        catch (Exception ex)
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = $"ファイル書き込みエラー: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// ディレクトリの内容を一覧表示します
    /// </summary>
    /// <param name="arguments">引数</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>実行結果</returns>
    public static async Task<ToolResult> ListDirectoryAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryGetValue("path", out var pathObj) || pathObj is not string path)
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = "pathパラメータが必要です"
            };
        }

        try
        {
            var entries = Directory.GetFileSystemEntries(path);
            var result = string.Join("\n", entries.Select(entry =>
            {
                var info = new FileInfo(entry);
                var type = info.Attributes.HasFlag(FileAttributes.Directory) ? "[DIR]" : "[FILE]";
                return $"{type} {Path.GetFileName(entry)}";
            }));

            return new ToolResult
            {
                IsSuccessful = true,
                Output = result
            };
        }
        catch (Exception ex)
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = $"ディレクトリ読み込みエラー: {ex.Message}"
            };
        }
    }
}