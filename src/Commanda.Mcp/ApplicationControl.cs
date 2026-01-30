using System.Diagnostics;
using System.Text.RegularExpressions;
using Commanda.Core;

namespace Commanda.Mcp;

/// <summary>
/// アプリケーション制御ツールの実装
/// </summary>
public static class ApplicationControl
{
    // 危険なパターンの定義
    private static readonly string[] DangerousPatterns = new[]
    {
        @"del\s+/[fq]\s+.*C:\\",
        @"rmdir\s+/[sq]\s+.*C:\\",
        @"format\s+",
        @"diskpart",
        @"reg\s+delete",
        @"net\s+user",
        @"net\s+localgroup",
        @"takeown",
        @"icacls.*\/grant.*administrators",
        @"powershell.*-EncodedCommand",
        @"powershell.*-enc",
        @"powershell.*IEX",
        @"powershell.*Invoke-Expression",
        @"cmd.*\/c.*del",
        @"cmd.*\/k.*del",
        @">.*nul.*2>&1.*del",
        @"fsutil\s+file\s+setzerodata",
        @"cipher\s+/w",
        @"vssadmin\s+delete",
        @"wbadmin\s+delete",
        @"bcdedit",
        @"bootrec"
    };

    // 危険な実行ファイルのリスト
    private static readonly string[] BlockedExecutables = new[]
    {
        "regedit.exe",
        "reg.exe",
        "format.com",
        "diskpart.exe",
        "vssadmin.exe",
        "wbadmin.exe",
        "bcdedit.exe",
        "bootrec.exe",
        "fsutil.exe",
        "cipher.exe",
        "takeown.exe",
        "icacls.exe",
        "net.exe",
        "net1.exe",
        "sc.exe",
        "schtasks.exe",
        "at.exe",
        "attrib.exe",
        "cacls.exe",
        "debug.exe",
        "edlin.exe",
        "debug64.exe",
        "edlin64.exe"
    };

    /// <summary>
    /// アプリケーションを起動します
    /// </summary>
    /// <param name="arguments">引数</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>実行結果</returns>
    public static async Task<ToolResult> LaunchApplicationAsync(
        Dictionary<string, object> arguments,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // パラメータ検証
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
                Error = "pathパラメータは空にできません"
            };
        }

        // 追加パラメータの取得
        var appArguments = arguments.TryGetValue("arguments", out var argsObj) && argsObj is string args
            ? args
            : string.Empty;

        var workingDirectory = arguments.TryGetValue("working_directory", out var workDirObj) && workDirObj is string workDir
            ? workDir
            : string.Empty;

        try
        {
            // セキュリティチェック
            var securityCheck = ValidateApplicationSecurity(path, appArguments);
            if (!securityCheck.IsValid)
            {
                return new ToolResult
                {
                    IsSuccessful = false,
                    Error = $"危険な操作が検出されました: {securityCheck.Reason}"
                };
            }

            // パスの検証と解決
            var resolvedPath = ResolveApplicationPath(path);
            if (string.IsNullOrEmpty(resolvedPath))
            {
                return new ToolResult
                {
                    IsSuccessful = false,
                    Error = $"アプリケーション '{path}' が見つかりません。パスを確認してください。"
                };
            }

            // 作業ディレクトリの検証
            if (!string.IsNullOrEmpty(workingDirectory) && !Directory.Exists(workingDirectory))
            {
                return new ToolResult
                {
                    IsSuccessful = false,
                    Error = $"作業ディレクトリ '{workingDirectory}' が存在しません。"
                };
            }

            // プロセス起動設定
            var startInfo = new ProcessStartInfo
            {
                FileName = resolvedPath,
                Arguments = appArguments,
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                startInfo.WorkingDirectory = workingDirectory;
            }

            // プロセス起動
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return new ToolResult
                {
                    IsSuccessful = false,
                    Error = "プロセスの起動に失敗しました。"
                };
            }

            // 短時間待機して起動を確認
            await Task.Delay(100, cancellationToken);

            if (process.HasExited && process.ExitCode != 0)
            {
                return new ToolResult
                {
                    IsSuccessful = false,
                    Error = $"アプリケーションが異常終了しました (Exit Code: {process.ExitCode})"
                };
            }

            return new ToolResult
            {
                IsSuccessful = true,
                Output = $"アプリケーションを起動しました (PID: {process.Id}, Path: {resolvedPath})"
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
                Error = $"アプリケーション起動エラー: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// アプリケーションを終了します
    /// </summary>
    /// <param name="arguments">引数</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>実行結果</returns>
    public static async Task<ToolResult> CloseApplicationAsync(
        Dictionary<string, object> arguments,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // パラメータ検証
        if (!arguments.TryGetValue("process_id", out var processIdObj) || processIdObj is not int processId)
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = "process_idパラメータが必要です（整数値）"
            };
        }

        if (processId <= 0)
        {
            return new ToolResult
            {
                IsSuccessful = false,
                Error = "process_idは正の整数である必要があります"
            };
        }

        try
        {
            // プロセスの取得
            Process? process = null;
            try
            {
                process = Process.GetProcessById(processId);
            }
            catch (ArgumentException)
            {
                return new ToolResult
                {
                    IsSuccessful = false,
                    Error = $"プロセスID {processId} が見つかりません。"
                };
            }

            using (process)
            {
                // システムプロセスのチェック
                if (IsSystemProcess(process))
                {
                    return new ToolResult
                    {
                        IsSuccessful = false,
                        Error = $"システムプロセス (PID: {processId}) は終了できません。"
                    };
                }

                var processName = process.ProcessName;

                // まず優雅に終了を試みる（メインウィンドウにクローズメッセージ送信）
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    process.CloseMainWindow();
                    
                    // 終了を待機（最大3秒）
                    var exited = await WaitForExitAsync(process, TimeSpan.FromSeconds(3), cancellationToken);
                    
                    if (exited)
                    {
                        return new ToolResult
                        {
                            IsSuccessful = true,
                            Output = $"アプリケーション '{processName}' (PID: {processId}) を正常に終了しました。"
                        };
                    }
                }

                // 強制終了
                process.Kill();
                
                // 強制終了の確認を待機
                var killed = await WaitForExitAsync(process, TimeSpan.FromSeconds(5), cancellationToken);
                
                if (killed)
                {
                    return new ToolResult
                    {
                        IsSuccessful = true,
                        Output = $"アプリケーション '{processName}' (PID: {processId}) を強制終了しました。"
                    };
                }
                else
                {
                    return new ToolResult
                    {
                        IsSuccessful = false,
                        Error = $"アプリケーション '{processName}' (PID: {processId}) の終了に失敗しました。"
                    };
                }
            }
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
                Error = $"アプリケーション終了エラー: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 実行中のアプリケーション一覧を取得します
    /// </summary>
    /// <param name="arguments">引数</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>実行結果</returns>
    public static async Task<ToolResult> GetRunningApplicationsAsync(
        Dictionary<string, object> arguments,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var processes = Process.GetProcesses()
                .Where(p => !string.IsNullOrEmpty(p.ProcessName))
                .Select(p =>
                {
                    // Safely access process properties that may throw exceptions
                    // for system processes or processes requiring elevation
                    long memoryMB = 0;
                    string? mainWindowTitle = null;

                    try
                    {
                        memoryMB = p.WorkingSet64 / (1024 * 1024);
                    }
                    catch
                    {
                        // Ignore - process may require elevation
                    }

                    try
                    {
                        mainWindowTitle = p.MainWindowTitle;
                    }
                    catch
                    {
                        // Ignore - process may require elevation
                    }

                    return new
                    {
                        p.Id,
                        p.ProcessName,
                        MainWindowTitle = mainWindowTitle,
                        MemoryMB = memoryMB
                    };
                })
                .OrderBy(p => p.ProcessName)
                .ToList();

            var result = new System.Text.StringBuilder();
            result.AppendLine($"実行中のアプリケーション: {processes.Count}件");
            result.AppendLine();
            result.AppendLine($"{"PID",-8} {"プロセス名",-25} {"ウィンドウタイトル",-40} {"メモリ(MB)",-12}");
            result.AppendLine(new string('-', 85));

            foreach (var process in processes.Take(100)) // 最大100件まで表示
            {
                var windowTitle = string.IsNullOrEmpty(process.MainWindowTitle) 
                    ? "(バックグラウンド)" 
                    : process.MainWindowTitle.Length > 37 
                        ? process.MainWindowTitle.Substring(0, 37) + "..."
                        : process.MainWindowTitle;

                result.AppendLine($"{process.Id,-8} {process.ProcessName,-25} {windowTitle,-40} {process.MemoryMB,-12}");
            }

            if (processes.Count > 100)
            {
                result.AppendLine();
                result.AppendLine($"... 他 {processes.Count - 100} 件のプロセスがあります");
            }

            return new ToolResult
            {
                IsSuccessful = true,
                Output = result.ToString()
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
                Error = $"プロセス一覧取得エラー: {ex.Message}"
            };
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// アプリケーションのセキュリティを検証します
    /// </summary>
    /// <param name="path">アプリケーションパス</param>
    /// <param name="arguments">引数</param>
    /// <returns>検証結果</returns>
    private static (bool IsValid, string? Reason) ValidateApplicationSecurity(string path, string arguments)
    {
        // 実行ファイル名の抽出
        var executableName = Path.GetFileName(path).ToLowerInvariant();

        // 危険な実行ファイルのチェック
        if (BlockedExecutables.Contains(executableName))
        {
            return (false, $"'{executableName}' は危険な実行ファイルのため起動できません。");
        }

        // 引数のセキュリティチェック
        var fullCommand = $"{path} {arguments}";
        foreach (var pattern in DangerousPatterns)
        {
            if (Regex.IsMatch(fullCommand, pattern, RegexOptions.IgnoreCase))
            {
                return (false, "危険なコマンドパターンが検出されました。");
            }
        }

        // cmd.exe と powershell.exe の特別なチェック
        if (executableName == "cmd.exe" || executableName == "powershell.exe" || executableName == "pwsh.exe")
        {
            // これらは許可するが、引数を厳密にチェック
            if (ContainsDangerousCommand(arguments))
            {
                return (false, "コマンドに危険な操作が含まれています。");
            }
        }

        return (true, null);
    }

    /// <summary>
    /// 危険なコマンドが含まれているかチェックします
    /// </summary>
    /// <param name="arguments">引数</param>
    /// <returns>危険なコマンドが含まれている場合はtrue</returns>
    private static bool ContainsDangerousCommand(string arguments)
    {
        var dangerousCommands = new[]
        {
            "del ", "erase ", "rmdir ", "rd ", "format ", "diskpart",
            "reg delete", "reg add", "net user", "net localgroup",
            "takeown", "icacls", "attrib -r -s -h",
            "fsutil", "cipher", "vssadmin", "wbadmin",
            "bcdedit", "bootrec", ">nul", "2>&1"
        };

        var lowerArgs = arguments.ToLowerInvariant();
        return dangerousCommands.Any(cmd => lowerArgs.Contains(cmd));
    }

    /// <summary>
    /// アプリケーションパスを解決します
    /// </summary>
    /// <param name="path">パス</param>
    /// <returns>解決されたパス（見つからない場合はnull）</returns>
    private static string? ResolveApplicationPath(string path)
    {
        // 絶対パスの場合
        if (File.Exists(path))
        {
            return Path.GetFullPath(path);
        }

        // 相対パスまたは実行ファイル名のみの場合
        if (!Path.IsPathRooted(path))
        {
            // PATH環境変数から検索
            var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            var pathDirs = pathEnv.Split(Path.PathSeparator);

            var extensions = new[] { ".exe", ".com", ".bat", ".cmd", "" };

            foreach (var dir in pathDirs)
            {
                if (string.IsNullOrWhiteSpace(dir))
                    continue;

                foreach (var ext in extensions)
                {
                    var fullPath = Path.Combine(dir, path + ext);
                    if (File.Exists(fullPath))
                    {
                        return Path.GetFullPath(fullPath);
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// システムプロセスかどうかを判定します
    /// </summary>
    /// <param name="process">プロセス</param>
    /// <returns>システムプロセスの場合はtrue</returns>
    private static bool IsSystemProcess(Process process)
    {
        var systemProcesses = new[]
        {
            "system", "registry", "smss", "csrss", "wininit",
            "services", "lsass", "svchost", "explorer", "winlogon",
            "fontdrvhost", "dwm", "memory compression", "secure system"
        };

        return systemProcesses.Contains(process.ProcessName.ToLowerInvariant());
    }

    /// <summary>
    /// プロセスの終了を非同期で待機します
    /// </summary>
    /// <param name="process">プロセス</param>
    /// <param name="timeout">タイムアウト</param>
    /// <param name="cancellationToken">キャンセレーショントークン</param>
    /// <returns>終了した場合はtrue</returns>
    private static async Task<bool> WaitForExitAsync(
        Process process,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        
        while (DateTime.UtcNow - startTime < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (process.HasExited)
            {
                return true;
            }

            await Task.Delay(100, cancellationToken);
        }

        return process.HasExited;
    }

    #endregion
}
