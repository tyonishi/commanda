namespace Commanda.Core;

/// <summary>
/// Unit of Workインターフェース
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// 実行ログリポジトリ
    /// </summary>
    IRepository<ExecutionLog> ExecutionLogs { get; }

    /// <summary>
    /// タスク履歴リポジトリ
    /// </summary>
    IRepository<TaskHistory> TaskHistories { get; }

    /// <summary>
    /// 拡張機能リポジトリ
    /// </summary>
    IRepository<ExtensionInfo> Extensions { get; }

    /// <summary>
    /// LLMプロバイダーリポジトリ
    /// </summary>
    IRepository<LlmProviderConfig> LlmProviders { get; }

    /// <summary>
    /// 変更を保存します
    /// </summary>
    /// <returns>影響を受けた行数</returns>
    Task<int> SaveChangesAsync();

    /// <summary>
    /// トランザクションを開始します
    /// </summary>
    /// <returns>タスク</returns>
    Task BeginTransactionAsync();

    /// <summary>
    /// トランザクションをコミットします
    /// </summary>
    /// <returns>タスク</returns>
    Task CommitAsync();

    /// <summary>
    /// トランザクションをロールバックします
    /// </summary>
    /// <returns>タスク</returns>
    Task RollbackAsync();
}