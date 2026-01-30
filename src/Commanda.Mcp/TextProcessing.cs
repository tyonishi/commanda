using System.Text;
using System.Text.RegularExpressions;
using Commanda.Core;

namespace Commanda.Mcp;

/// <summary>
/// テキスト処理ツールの実装
/// </summary>
public static class TextProcessing
{
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB
    private static readonly string[] BlockedPaths = new[]
    {
        @"C:\Windows",
        @"C:\Program Files",
        @"C:\Program Files (x86)",
        @"C:\ProgramData",
        @"C:\Users\All Users",
        @"C:\Users\Default",
        @"C:\Users\Public",
        @"C:\$Recycle.Bin",
        @"C:\System Volume Information",
        @"C:\Boot",
        @"C:\Config.Msi",
        @"C:\Recovery",
        @"C:\inetpub",
        @"/etc",
        @"/bin",
        @"/sbin",
        @"/usr/bin",
        @"/usr/sbin",
        @"/var/log",
        @"/var/spool",
        @"/proc",
        @"/sys",
        @"/dev",
        @"/boot",
        @"/root",
        @"/tmp"
    };

    /// <summary>
    /// テキストファイルを読み込みます
    /// </summary>
    /// <param name="arguments">引数</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>実行結果</returns>
    public static async Task<ToolResult> ReadTextFileAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!arguments.TryGetValue("path", out var pathObj) || pathObj is not string path)
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = "pathパラメータが必要です"
            };
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = "pathパラメータが空です"
            };
        }

        if (IsBlockedPath(path))
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = "指定されたパスは許可されていません"
            };
        }

        try
        {
            var fileInfo = new FileInfo(path);
            if (!fileInfo.Exists)
            {
                return new ToolResult
                {
                    IsSuccessful = false,
                    Error = "ファイルが見つかりません"
                };
            }

            if (fileInfo.Length > MaxFileSize)
            {
                return new ToolResult
                {
                    IsSuccessful = false,
                    Error = "ファイルが大きすぎます（最大10MB）"
                };
            }

            var encoding = GetEncodingFromArguments(arguments);
            var content = await File.ReadAllTextAsync(path, encoding, cancellationToken);

            return new ToolResult
            {
                IsSuccessful = true,
                Output = content
            };
        }
        catch (OperationCanceledException)
        {
            throw;
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
    /// テキストファイルに書き込みます
    /// </summary>
    /// <param name="arguments">引数</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>実行結果</returns>
    public static async Task<ToolResult> WriteTextFileAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

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

        if (string.IsNullOrWhiteSpace(path))
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = "pathパラメータが空です"
            };
        }

        if (IsBlockedPath(path))
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = "指定されたパスは許可されていません"
            };
        }

        if (content.Length > MaxFileSize)
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = "コンテンツが大きすぎます（最大10MB）"
            };
        }

        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // バックアップオプション
            if (arguments.TryGetValue("create_backup", out var backupObj) && 
                backupObj is bool createBackup && 
                createBackup && 
                File.Exists(path))
            {
                var backupPath = path + ".backup";
                File.Copy(path, backupPath, overwrite: true);
            }

            var encoding = GetEncodingFromArguments(arguments);
            await File.WriteAllTextAsync(path, content, encoding, cancellationToken);

            return new ToolResult
            {
                IsSuccessful = true,
                Output = "ファイルが正常に書き込まれました"
            };
        }
        catch (OperationCanceledException)
        {
            throw;
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
    /// ファイルに内容を追加します
    /// </summary>
    /// <param name="arguments">引数</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>実行結果</returns>
    public static async Task<ToolResult> AppendToFileAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

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

        if (string.IsNullOrWhiteSpace(path))
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = "pathパラメータが空です"
            };
        }

        if (IsBlockedPath(path))
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = "指定されたパスは許可されていません"
            };
        }

        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 既存ファイルのサイズチェック
            if (File.Exists(path))
            {
                var fileInfo = new FileInfo(path);
                if (fileInfo.Length + content.Length > MaxFileSize)
                {
                    return new ToolResult
                    {
                        IsSuccessful = false,
                        Error = "ファイルサイズが制限を超えます（最大10MB）"
                    };
                }
            }

            var encoding = GetEncodingFromArguments(arguments);
            await File.AppendAllTextAsync(path, content, encoding, cancellationToken);

            return new ToolResult
            {
                IsSuccessful = true,
                Output = "内容が正常に追加されました"
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = $"ファイル追加エラー: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// ファイル内を検索します
    /// </summary>
    /// <param name="arguments">引数</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>実行結果</returns>
    public static async Task<ToolResult> SearchInFileAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!arguments.TryGetValue("path", out var pathObj) || pathObj is not string path)
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = "pathパラメータが必要です"
            };
        }

        if (!arguments.TryGetValue("pattern", out var patternObj) || patternObj is not string pattern)
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = "patternパラメータが必要です"
            };
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = "pathパラメータが空です"
            };
        }

        if (string.IsNullOrWhiteSpace(pattern))
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = "patternパラメータが空です"
            };
        }

        if (IsBlockedPath(path))
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = "指定されたパスは許可されていません"
            };
        }

        try
        {
            var fileInfo = new FileInfo(path);
            if (!fileInfo.Exists)
            {
                return new ToolResult
                {
                    IsSuccessful = false,
                    Error = "ファイルが見つかりません"
                };
            }

            if (fileInfo.Length > MaxFileSize)
            {
                return new ToolResult
                {
                    IsSuccessful = false,
                    Error = "ファイルが大きすぎます（最大10MB）"
                };
            }

            var encoding = GetEncodingFromArguments(arguments);
            var content = await File.ReadAllTextAsync(path, encoding, cancellationToken);
            var useRegex = arguments.TryGetValue("use_regex", out var regexObj) && regexObj is bool useRegexFlag && useRegexFlag;

            var matches = new List<string>();
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            for (var i = 0; i < lines.Length; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var line = lines[i];
                var isMatch = useRegex
                    ? Regex.IsMatch(line, pattern)
                    : line.Contains(pattern, StringComparison.OrdinalIgnoreCase);

                if (isMatch)
                {
                    matches.Add($"Line {i + 1}: {line}");
                }
            }

            if (matches.Count == 0)
            {
                return new ToolResult
                {
                    IsSuccessful = true,
                    Output = "一致する行が見つかりませんでした"
                };
            }

            return new ToolResult
            {
                IsSuccessful = true,
                Output = string.Join("\n", matches)
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (RegexParseException ex)
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = $"正規表現の構文エラー: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = $"検索エラー: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// ファイル内のテキストを置換します
    /// </summary>
    /// <param name="arguments">引数</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>実行結果</returns>
    public static async Task<ToolResult> ReplaceInFileAsync(Dictionary<string, object> arguments, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!arguments.TryGetValue("path", out var pathObj) || pathObj is not string path)
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = "pathパラメータが必要です"
            };
        }

        if (!arguments.TryGetValue("old_text", out var oldTextObj) || oldTextObj is not string oldText)
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = "old_textパラメータが必要です"
            };
        }

        if (!arguments.TryGetValue("new_text", out var newTextObj) || newTextObj is not string newText)
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = "new_textパラメータが必要です"
            };
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = "pathパラメータが空です"
            };
        }

        if (IsBlockedPath(path))
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = "指定されたパスは許可されていません"
            };
        }

        try
        {
            var fileInfo = new FileInfo(path);
            if (!fileInfo.Exists)
            {
                return new ToolResult
                {
                    IsSuccessful = false,
                    Error = "ファイルが見つかりません"
                };
            }

            if (fileInfo.Length > MaxFileSize)
            {
                return new ToolResult
                {
                    IsSuccessful = false,
                    Error = "ファイルが大きすぎます（最大10MB）"
                };
            }

            var encoding = GetEncodingFromArguments(arguments);
            var content = await File.ReadAllTextAsync(path, encoding, cancellationToken);

            var useRegex = arguments.TryGetValue("use_regex", out var regexObj) && regexObj is bool useRegexFlag && useRegexFlag;
            var createBackup = arguments.TryGetValue("create_backup", out var backupObj) && backupObj is bool createBackupFlag && createBackupFlag;

            string newContent;
            int replacementCount;

            if (useRegex)
            {
                try
                {
                    var regex = new Regex(oldText);
                    replacementCount = regex.Matches(content).Count;
                    newContent = regex.Replace(content, newText);
                }
                catch (RegexParseException ex)
                {
                    return new ToolResult
                    {
                        IsSuccessful = false,
                        Error = $"正規表現の構文エラー: {ex.Message}"
                    };
                }
            }
            else
            {
                replacementCount = content.Split(new[] { oldText }, StringSplitOptions.None).Length - 1;
                newContent = content.Replace(oldText, newText);
            }

            // バックアップ作成
            if (createBackup)
            {
                var backupPath = path + ".backup";
                await File.WriteAllTextAsync(backupPath, content, encoding, cancellationToken);
            }

            await File.WriteAllTextAsync(path, newContent, encoding, cancellationToken);

            return new ToolResult
            {
                IsSuccessful = true,
                Output = $"{replacementCount}箇所を置換しました"
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = $"置換エラー: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// パスがブロックされたパスかどうかを確認します
    /// </summary>
    /// <param name="path">確認するパス</param>
    /// <returns>ブロックされている場合はtrue</returns>
    private static bool IsBlockedPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return true;
        }

        var fullPath = Path.GetFullPath(path);

        foreach (var blockedPath in BlockedPaths)
        {
            if (fullPath.StartsWith(blockedPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 引数からエンコーディングを取得します
    /// </summary>
    /// <param name="arguments">引数</param>
    /// <returns>エンコーディング</returns>
    private static Encoding GetEncodingFromArguments(Dictionary<string, object> arguments)
    {
        if (arguments.TryGetValue("encoding", out var encodingObj) && encodingObj is string encodingName)
        {
            return encodingName.ToUpperInvariant() switch
            {
                "UTF-8" => Encoding.UTF8,
                "UTF8" => Encoding.UTF8,
                "UTF-16" => Encoding.Unicode,
                "UTF16" => Encoding.Unicode,
                "UTF-16LE" => Encoding.Unicode,
                "UTF-16BE" => Encoding.BigEndianUnicode,
                "UTF-32" => Encoding.UTF32,
                "UTF32" => Encoding.UTF32,
                "ASCII" => Encoding.ASCII,
                "SHIFT-JIS" => Encoding.GetEncoding("shift-jis"),
                "SHIFT_JIS" => Encoding.GetEncoding("shift-jis"),
                "EUC-JP" => Encoding.GetEncoding("euc-jp"),
                "EUC_JP" => Encoding.GetEncoding("euc-jp"),
                "ISO-2022-JP" => Encoding.GetEncoding("iso-2022-jp"),
                "ISO_2022_JP" => Encoding.GetEncoding("iso-2022-jp"),
                "LATIN1" => Encoding.Latin1,
                "ISO-8859-1" => Encoding.Latin1,
                _ => Encoding.UTF8
            };
        }

        return Encoding.UTF8;
    }
}
