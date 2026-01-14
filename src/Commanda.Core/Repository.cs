using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Commanda.Core;

/// <summary>
/// EF Coreを使用したリポジトリ実装
/// </summary>
/// <typeparam name="T">エンティティ型</typeparam>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly CommandaDbContext _context;
    protected readonly DbSet<T> _dbSet;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="context">データベースコンテキスト</param>
    public Repository(CommandaDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<T>();
    }

    /// <summary>
    /// IDでエンティティを取得します
    /// </summary>
    /// <param name="id">ID</param>
    /// <returns>エンティティ</returns>
    public async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    /// <summary>
    /// すべてのエンティティを取得します
    /// </summary>
    /// <returns>エンティティのコレクション</returns>
    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    /// <summary>
    /// 条件に一致するエンティティを取得します
    /// </summary>
    /// <param name="predicate">条件式</param>
    /// <returns>エンティティのコレクション</returns>
    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    /// <summary>
    /// 条件に一致する最初のエンティティを取得します
    /// </summary>
    /// <param name="predicate">条件式</param>
    /// <returns>エンティティ</returns>
    public async Task<T?> FindFirstAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate);
    }

    /// <summary>
    /// エンティティを追加します
    /// </summary>
    /// <param name="entity">エンティティ</param>
    /// <returns>追加されたエンティティ</returns>
    public async Task<T> AddAsync(T entity)
    {
        var entry = await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entry.Entity;
    }

    /// <summary>
    /// エンティティを更新します
    /// </summary>
    /// <param name="entity">エンティティ</param>
    /// <returns>タスク</returns>
    public async Task UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// エンティティを削除します
    /// </summary>
    /// <param name="entity">エンティティ</param>
    /// <returns>タスク</returns>
    public async Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// IDでエンティティを削除します
    /// </summary>
    /// <param name="id">ID</param>
    /// <returns>タスク</returns>
    public async Task DeleteByIdAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            await DeleteAsync(entity);
        }
    }

    /// <summary>
    /// 条件に一致するエンティティが存在するかを確認します
    /// </summary>
    /// <param name="predicate">条件式</param>
    /// <returns>存在するかどうか</returns>
    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    /// <summary>
    /// 条件に一致するエンティティの数をカウントします
    /// </summary>
    /// <param name="predicate">条件式</param>
    /// <returns>カウント数</returns>
    public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.CountAsync(predicate);
    }
}