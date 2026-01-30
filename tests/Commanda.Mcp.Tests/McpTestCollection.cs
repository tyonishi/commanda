using Xunit;

namespace Commanda.Mcp.Tests;

/// <summary>
/// MCPテストのコレクション定義
/// ファイルシステムやプロセス操作を行うテストをシリアル実行し、
/// リソース競合を防止します
/// </summary>
[CollectionDefinition("MCP Tests", DisableParallelization = true)]
public class McpTestCollection : ICollectionFixture<McpTestFixture>
{
    // このクラスはマーカーとして使用されます
}

/// <summary>
/// MCPテスト用のフィクスチャ
/// </summary>
public class McpTestFixture : IDisposable
{
    private readonly string _testBasePath;

    public McpTestFixture()
    {
        // テスト用の一意なベースパスを生成
        _testBasePath = Path.Combine(
            Path.GetTempPath(),
            $"CommandaTests_{Guid.NewGuid()}");
        
        Directory.CreateDirectory(_testBasePath);
    }

    public string GetTestPath(string fileName)
    {
        return Path.Combine(_testBasePath, fileName);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testBasePath))
            {
                Directory.Delete(_testBasePath, recursive: true);
            }
        }
        catch
        {
            // クリーンアップエラーを無視
        }
    }
}
