namespace Commanda.Core;

/// <summary>
/// Unit of Work実装
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly CommandaDbContext _context;
    private bool _disposed = false;

    // リポジトリの遅延初期化
    private IRepository<ExecutionLog>? _executionLogs;
    private IRepository<TaskHistory>? _taskHistories;
    private IRepository<ExtensionInfo>? _extensions;
    private IRepository<LlmProviderConfig>? _llmProviders;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="context">データベースコンテキスト</param>
    public UnitOfWork(CommandaDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// 実行ログリポジトリ
    /// </summary>
    public IRepository<ExecutionLog> ExecutionLogs =>
        _executionLogs ??= new Repository<ExecutionLog>(_context);

    /// <summary>
    /// タスク履歴リポジトリ
    /// </summary>
    public IRepository<TaskHistory> TaskHistories =>
        _taskHistories ??= new Repository<TaskHistory>(_context);

    /// <summary>
    /// 拡張機能リポジトリ
    /// </summary>
    public IRepository<ExtensionInfo> Extensions =>
        _extensions ??= new Repository<ExtensionInfo>(_context);

    /// <summary>
    /// LLMプロバイダーリポジトリ
    /// </summary>
    public IRepository<LlmProviderConfig> LlmProviders =>
        _llmProviders ??= new Repository<LlmProviderConfig>(_context);

    /// <summary>
    /// 変更を保存します
    /// </summary>
    /// <returns>影響を受けた行数</returns>
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    /// <summary>
    /// トランザクションを開始します
    /// </summary>
    /// <returns>タスク</returns>
    public async Task BeginTransactionAsync()
    {
        await _context.Database.BeginTransactionAsync();
    }

    /// <summary>
    /// トランザクションをコミットします
    /// </summary>
    /// <returns>タスク</returns>
    public async Task CommitAsync()
    {
        await _context.Database.CommitTransactionAsync();
    }

    /// <summary>
    /// トランザクションをロールバックします
    /// </summary>
    /// <returns>タスク</returns>
    public async Task RollbackAsync()
    {
        await _context.Database.RollbackTransactionAsync();
    }

    /// <summary>
    /// リソースを解放します
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// リソースを解放します
    /// </summary>
    /// <param name="disposing">マネージドリソースを解放するかどうか</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            _disposed = true;
        }
    }
}