using Xunit;
using Commanda.Core;
using Commanda.Mcp;
using System.Runtime.Versioning;

namespace Commanda.Mcp.Tests;

[Collection("MCP Tests")] // シリアル実行でリソース競合を防止
[SupportedOSPlatform("windows")]
public class ApplicationControlTests : IDisposable
{
    private readonly List<string> _tempFiles = new();
    private readonly List<System.Diagnostics.Process> _startedProcesses = new();

    public void Dispose()
    {
        // Cleanup temporary files
        foreach (var file in _tempFiles.ToList())
        {
            CleanupFileWithRetry(file, maxRetries: 3);
        }
        _tempFiles.Clear();

        // Cleanup started processes
        foreach (var process in _startedProcesses.ToList())
        {
            try
            {
                if (process != null && !process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit(2000);
                }
                process?.Dispose();
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        _startedProcesses.Clear();
    }

    private void CleanupFileWithRetry(string filePath, int maxRetries)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                return; // Success
            }
            catch (IOException)
            {
                // File might be locked, wait and retry
                if (i < maxRetries - 1)
                {
                    Thread.Sleep(100 * (i + 1));
                }
            }
            catch
            {
                // Ignore other cleanup errors
                return;
            }
        }
    }

    [Fact]
    public async Task LaunchApplicationAsync_ValidPath_ReturnsSuccess()
    {
        // Arrange
        var testFile = Path.GetTempFileName();
        _tempFiles.Add(testFile);
        var arguments = new Dictionary<string, object>
        {
            ["path"] = "notepad.exe",
            ["arguments"] = testFile
        };

        // Act
        var result = await ApplicationControl.LaunchApplicationAsync(arguments, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Output);
        Assert.Contains("起動", result.Output.ToString());
    }

    [Fact]
    public async Task LaunchApplicationAsync_InvalidPath_ReturnsError()
    {
        // Arrange
        var arguments = new Dictionary<string, object>
        {
            ["path"] = "nonexistent_application_xyz.exe"
        };

        // Act
        var result = await ApplicationControl.LaunchApplicationAsync(arguments, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.NotNull(result.Error);
        Assert.Contains("見つかりません", result.Error);
    }

    [Fact]
    public async Task LaunchApplicationAsync_BlockedApplication_ReturnsError()
    {
        // Arrange - 危険なコマンドパターンをテスト（実際には実行されない）
        var arguments = new Dictionary<string, object>
        {
            ["path"] = "cmd.exe",
            ["arguments"] = "/c format C: /y"
        };

        // Act
        var result = await ApplicationControl.LaunchApplicationAsync(arguments, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.NotNull(result.Error);
        Assert.Contains("危険", result.Error);
    }

    [Fact]
    public async Task LaunchApplicationAsync_MissingPathParameter_ReturnsError()
    {
        // Arrange
        var arguments = new Dictionary<string, object>();

        // Act
        var result = await ApplicationControl.LaunchApplicationAsync(arguments, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.NotNull(result.Error);
        Assert.Contains("pathパラメータ", result.Error);
    }

    [Fact]
    public async Task CloseApplicationAsync_RunningProcess_ReturnsSuccess()
    {
        // Arrange - Start notepad first
        var testFile = Path.GetTempFileName();
        _tempFiles.Add(testFile);
        var startArguments = new Dictionary<string, object>
        {
            ["path"] = "notepad.exe",
            ["arguments"] = testFile
        };
        var startResult = await ApplicationControl.LaunchApplicationAsync(startArguments, CancellationToken.None);
        Assert.True(startResult.IsSuccessful);

        // Get the process ID from the result
        var processId = ExtractProcessId(startResult.Output?.ToString());
        Assert.True(processId > 0, "Process ID should be extracted from result");

        var closeArguments = new Dictionary<string, object>
        {
            ["process_id"] = processId
        };

        // Act
        var result = await ApplicationControl.CloseApplicationAsync(closeArguments, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Output);
        Assert.Contains("終了", result.Output.ToString());
    }

    [Fact]
    public async Task CloseApplicationAsync_NonExistentProcess_ReturnsError()
    {
        // Arrange
        var arguments = new Dictionary<string, object>
        {
            ["process_id"] = 999999 // Non-existent process ID
        };

        // Act
        var result = await ApplicationControl.CloseApplicationAsync(arguments, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.NotNull(result.Error);
        Assert.Contains("見つかりません", result.Error);
    }

    [Fact]
    public async Task CloseApplicationAsync_MissingParameters_ReturnsError()
    {
        // Arrange
        var arguments = new Dictionary<string, object>();

        // Act
        var result = await ApplicationControl.CloseApplicationAsync(arguments, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.NotNull(result.Error);
        Assert.Contains("process_id", result.Error);
    }

    [Fact]
    public async Task GetRunningApplicationsAsync_ReturnsList()
    {
        // Arrange
        var arguments = new Dictionary<string, object>();

        // Act
        var result = await ApplicationControl.GetRunningApplicationsAsync(arguments, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Output);
        var output = result.Output?.ToString();
        Assert.NotNull(output);
        Assert.NotEmpty(output);
    }

    [Fact]
    public async Task LaunchApplicationAsync_WithWorkingDirectory_ReturnsSuccess()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var arguments = new Dictionary<string, object>
        {
            ["path"] = "cmd.exe",
            ["arguments"] = "/c echo test",
            ["working_directory"] = tempDir
        };

        // Act
        var result = await ApplicationControl.LaunchApplicationAsync(arguments, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.NotNull(result.Output);
    }

    [Fact]
    public async Task LaunchApplicationAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var arguments = new Dictionary<string, object>
        {
            ["path"] = "notepad.exe"
        };

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await ApplicationControl.LaunchApplicationAsync(arguments, cts.Token);
        });
    }

    #region Helper Methods

    private int ExtractProcessId(string? output)
    {
        if (string.IsNullOrEmpty(output))
            return 0;

        // Extract process ID from output like "アプリケーションを起動しました (PID: 12345)"
        var match = System.Text.RegularExpressions.Regex.Match(output, @"PID[\s:]+(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var processId))
        {
            return processId;
        }

        return 0;
    }

    #endregion
}
