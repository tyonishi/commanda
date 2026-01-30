using Xunit;
using Commanda.Core;
using Commanda.Mcp;
using System.Runtime.Versioning;

namespace Commanda.Mcp.Tests;

[Collection("MCP Tests")] // シリアル実行でリソース競合を防止
[SupportedOSPlatform("windows")]
public class TextProcessingTests : IDisposable
{
    private readonly List<string> _tempFiles = new();

    public void Dispose()
    {
        // Cleanup temporary files with retry logic
        foreach (var file in _tempFiles.ToList())
        {
            CleanupFileWithRetry(file, maxRetries: 3);
        }
        _tempFiles.Clear();
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
                // Also cleanup backup files if they exist
                var backupPath = filePath + ".backup";
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
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
    public async Task ReadTextFileAsync_ValidFile_ReturnsContent()
    {
        // Arrange
        var testFile = Path.GetTempFileName();
        _tempFiles.Add(testFile);
        var testContent = "Hello, World!\nThis is a test file.\n日本語も読めます。";
        await File.WriteAllTextAsync(testFile, testContent);

        var arguments = new Dictionary<string, object>
        {
            ["path"] = testFile
        };

        // Act
        var result = await TextProcessing.ReadTextFileAsync(arguments, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(testContent, result.Output);
    }

    [Fact]
    public async Task ReadTextFileAsync_NonExistentFile_ReturnsError()
    {
        // Arrange
        var arguments = new Dictionary<string, object>
        {
            ["path"] = "nonexistent_file.txt"
        };

        // Act
        var result = await TextProcessing.ReadTextFileAsync(arguments, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Contains("見つかりません", result.Error);
    }

    [Fact]
    public async Task ReadTextFileAsync_LargeFile_ReturnsError()
    {
        // Arrange - Create a file larger than 10MB limit
        var testFile = Path.GetTempFileName();
        _tempFiles.Add(testFile);
        var largeContent = new string('x', 11 * 1024 * 1024); // 11MB
        await File.WriteAllTextAsync(testFile, largeContent);

        var arguments = new Dictionary<string, object>
        {
            ["path"] = testFile
        };

        // Act
        var result = await TextProcessing.ReadTextFileAsync(arguments, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Contains("大きすぎます", result.Error);
    }

    [Fact]
    public async Task ReadTextFileAsync_MissingPathParameter_ReturnsError()
    {
        // Arrange
        var arguments = new Dictionary<string, object>();

        // Act
        var result = await TextProcessing.ReadTextFileAsync(arguments, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Contains("pathパラメータ", result.Error);
    }

    [Fact]
    public async Task WriteTextFileAsync_ValidContent_ReturnsSuccess()
    {
        // Arrange
        var testFile = Path.Combine(Path.GetTempPath(), $"test_write_{Guid.NewGuid()}.txt");
        _tempFiles.Add(testFile);
        var testContent = "Test content to write\nLine 2\nLine 3";

        var arguments = new Dictionary<string, object>
        {
            ["path"] = testFile,
            ["content"] = testContent
        };

        // Act
        var result = await TextProcessing.WriteTextFileAsync(arguments, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Contains("正常に書き込まれました", result.Output?.ToString());

        // Verify file content
        var actualContent = await File.ReadAllTextAsync(testFile);
        Assert.Equal(testContent, actualContent);
    }

    [Fact]
    public async Task WriteTextFileAsync_DangerousPath_ReturnsError()
    {
        // Arrange - Try to write to system directory
        var arguments = new Dictionary<string, object>
        {
            ["path"] = @"C:\Windows\System32\test_file.txt",
            ["content"] = "test"
        };

        // Act
        var result = await TextProcessing.WriteTextFileAsync(arguments, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Contains("許可されていません", result.Error);
    }

    [Fact]
    public async Task WriteTextFileAsync_MissingParameters_ReturnsError()
    {
        // Arrange
        var arguments = new Dictionary<string, object>
        {
            ["path"] = "test.txt"
            // Missing content parameter
        };

        // Act
        var result = await TextProcessing.WriteTextFileAsync(arguments, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Contains("contentパラメータ", result.Error);
    }

    [Fact]
    public async Task AppendToFileAsync_ValidContent_ReturnsSuccess()
    {
        // Arrange
        var testFile = Path.GetTempFileName();
        _tempFiles.Add(testFile);
        var initialContent = "Initial content\n";
        var appendContent = "Appended content";
        await File.WriteAllTextAsync(testFile, initialContent);

        var arguments = new Dictionary<string, object>
        {
            ["path"] = testFile,
            ["content"] = appendContent
        };

        // Act
        var result = await TextProcessing.AppendToFileAsync(arguments, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);

        // Verify file content
        var actualContent = await File.ReadAllTextAsync(testFile);
        Assert.Equal(initialContent + appendContent, actualContent);
    }

    [Fact]
    public async Task AppendToFileAsync_NewFile_CreatesFile()
    {
        // Arrange
        var testFile = Path.Combine(Path.GetTempPath(), $"test_append_{Guid.NewGuid()}.txt");
        _tempFiles.Add(testFile);
        var appendContent = "New file content";

        var arguments = new Dictionary<string, object>
        {
            ["path"] = testFile,
            ["content"] = appendContent
        };

        // Act
        var result = await TextProcessing.AppendToFileAsync(arguments, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.True(File.Exists(testFile));

        var actualContent = await File.ReadAllTextAsync(testFile);
        Assert.Equal(appendContent, actualContent);
    }

    [Fact]
    public async Task SearchInFileAsync_ExistingPattern_ReturnsMatches()
    {
        // Arrange
        var testFile = Path.GetTempFileName();
        _tempFiles.Add(testFile);
        var testContent = "Line 1: Hello World\nLine 2: Hello Universe\nLine 3: Goodbye World";
        await File.WriteAllTextAsync(testFile, testContent);

        var arguments = new Dictionary<string, object>
        {
            ["path"] = testFile,
            ["pattern"] = "Hello"
        };

        // Act
        var result = await TextProcessing.SearchInFileAsync(arguments, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        var output = result.Output?.ToString();
        Assert.NotNull(output);
        Assert.Contains("Line 1", output);
        Assert.Contains("Line 2", output);
        Assert.DoesNotContain("Line 3", output);
    }

    [Fact]
    public async Task SearchInFileAsync_RegexPattern_ReturnsMatches()
    {
        // Arrange
        var testFile = Path.GetTempFileName();
        _tempFiles.Add(testFile);
        var testContent = "Email: test@example.com\nPhone: 123-456-7890\nEmail: user@domain.org";
        await File.WriteAllTextAsync(testFile, testContent);

        var arguments = new Dictionary<string, object>
        {
            ["path"] = testFile,
            ["pattern"] = @"\w+@\w+\.\w+",
            ["use_regex"] = true
        };

        // Act
        var result = await TextProcessing.SearchInFileAsync(arguments, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        var output = result.Output?.ToString();
        Assert.NotNull(output);
        Assert.Contains("test@example.com", output);
        Assert.Contains("user@domain.org", output);
    }

    [Fact]
    public async Task SearchInFileAsync_NoMatches_ReturnsEmpty()
    {
        // Arrange
        var testFile = Path.GetTempFileName();
        _tempFiles.Add(testFile);
        var testContent = "Line 1\nLine 2\nLine 3";
        await File.WriteAllTextAsync(testFile, testContent);

        var arguments = new Dictionary<string, object>
        {
            ["path"] = testFile,
            ["pattern"] = "NonExistentPattern"
        };

        // Act
        var result = await TextProcessing.SearchInFileAsync(arguments, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        var output = result.Output?.ToString();
        Assert.NotNull(output);
        Assert.Contains("見つかりません", output);
    }

    [Fact]
    public async Task ReplaceInFileAsync_ValidReplacement_ReturnsSuccess()
    {
        // Arrange
        var testFile = Path.GetTempFileName();
        _tempFiles.Add(testFile);
        var testContent = "Hello World\nHello Universe\nGoodbye World";
        await File.WriteAllTextAsync(testFile, testContent);

        var arguments = new Dictionary<string, object>
        {
            ["path"] = testFile,
            ["old_text"] = "Hello",
            ["new_text"] = "Hi"
        };

        // Act
        var result = await TextProcessing.ReplaceInFileAsync(arguments, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);

        // Verify file content
        var actualContent = await File.ReadAllTextAsync(testFile);
        Assert.Equal("Hi World\nHi Universe\nGoodbye World", actualContent);
    }

    [Fact]
    public async Task ReplaceInFileAsync_RegexReplacement_ReturnsSuccess()
    {
        // Arrange
        var testFile = Path.GetTempFileName();
        _tempFiles.Add(testFile);
        var testContent = "Email: test@example.com\nEmail: user@domain.org";
        await File.WriteAllTextAsync(testFile, testContent);

        var arguments = new Dictionary<string, object>
        {
            ["path"] = testFile,
            ["old_text"] = @"\w+@\w+\.\w+",
            ["new_text"] = "[EMAIL REDACTED]",
            ["use_regex"] = true
        };

        // Act
        var result = await TextProcessing.ReplaceInFileAsync(arguments, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);

        // Verify file content
        var actualContent = await File.ReadAllTextAsync(testFile);
        Assert.Equal("Email: [EMAIL REDACTED]\nEmail: [EMAIL REDACTED]", actualContent);
    }

    [Fact]
    public async Task ReplaceInFileAsync_DangerousPath_ReturnsError()
    {
        // Arrange
        var arguments = new Dictionary<string, object>
        {
            ["path"] = @"C:\Windows\System32\test.txt",
            ["old_text"] = "old",
            ["new_text"] = "new"
        };

        // Act
        var result = await TextProcessing.ReplaceInFileAsync(arguments, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccessful);
        Assert.Contains("許可されていません", result.Error);
    }

    [Fact]
    public async Task ReplaceInFileAsync_CreatesBackup_ReturnsSuccess()
    {
        // Arrange
        var testFile = Path.GetTempFileName();
        _tempFiles.Add(testFile);
        var backupFile = testFile + ".backup";
        _tempFiles.Add(backupFile);
        var testContent = "Original content";
        await File.WriteAllTextAsync(testFile, testContent);

        var arguments = new Dictionary<string, object>
        {
            ["path"] = testFile,
            ["old_text"] = "Original",
            ["new_text"] = "Modified",
            ["create_backup"] = true
        };

        // Act
        var result = await TextProcessing.ReplaceInFileAsync(arguments, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.True(File.Exists(backupFile));

        var backupContent = await File.ReadAllTextAsync(backupFile);
        Assert.Equal(testContent, backupContent);
    }

    [Fact]
    public async Task WriteTextFileAsync_WithEncoding_ReturnsSuccess()
    {
        // Arrange
        var testFile = Path.Combine(Path.GetTempPath(), $"test_encoding_{Guid.NewGuid()}.txt");
        _tempFiles.Add(testFile);
        var testContent = "日本語コンテンツ\n中文内容\n한국어 내용";

        var arguments = new Dictionary<string, object>
        {
            ["path"] = testFile,
            ["content"] = testContent,
            ["encoding"] = "UTF-8"
        };

        // Act
        var result = await TextProcessing.WriteTextFileAsync(arguments, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);

        // Verify with UTF-8 encoding
        var actualContent = await File.ReadAllTextAsync(testFile, System.Text.Encoding.UTF8);
        Assert.Equal(testContent, actualContent);
    }

    [Fact]
    public async Task ReadTextFileAsync_WithSpecificEncoding_ReturnsContent()
    {
        // Arrange
        var testFile = Path.GetTempFileName();
        _tempFiles.Add(testFile);
        var testContent = "日本語テスト";
        await File.WriteAllTextAsync(testFile, testContent, System.Text.Encoding.UTF8);

        var arguments = new Dictionary<string, object>
        {
            ["path"] = testFile,
            ["encoding"] = "UTF-8"
        };

        // Act
        var result = await TextProcessing.ReadTextFileAsync(arguments, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccessful);
        Assert.Equal(testContent, result.Output);
    }

    [Fact]
    public async Task SearchInFileAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var arguments = new Dictionary<string, object>
        {
            ["path"] = "test.txt",
            ["pattern"] = "test"
        };

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await TextProcessing.SearchInFileAsync(arguments, cts.Token);
        });
    }
}
