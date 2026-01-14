using System.Linq.Expressions;

namespace Commanda.Core;

/// <summary>
/// 汎用リポジトリインターフェース
/// </summary>
/// <typeparam name="T">エンティティ型</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// IDでエンティティを取得します
    /// </summary>
    /// <param name="id">ID</param>
    /// <returns>エンティティ</returns>
    Task<T?> GetByIdAsync(int id);

    /// <summary>
    /// すべてのエンティティを取得します
    /// </summary>
    /// <returns>エンティティのコレクション</returns>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// 条件に一致するエンティティを取得します
    /// </summary>
    /// <param name="predicate">条件式</param>
    /// <returns>エンティティのコレクション</returns>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// 条件に一致する最初のエンティティを取得します
    /// </summary>
    /// <param name="predicate">条件式</param>
    /// <returns>エンティティ</returns>
    Task<T?> FindFirstAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// エンティティを追加します
    /// </summary>
    /// <param name="entity">エンティティ</param>
    /// <returns>追加されたエンティティ</returns>
    Task<T> AddAsync(T entity);

    /// <summary>
    /// エンティティを更新します
    /// </summary>
    /// <param name="entity">エンティティ</param>
    /// <returns>タスク</returns>
    Task UpdateAsync(T entity);

    /// <summary>
    /// エンティティを削除します
    /// </summary>
    /// <param name="entity">エンティティ</param>
    /// <returns>タスク</returns>
    Task DeleteAsync(T entity);

    /// <summary>
    /// IDでエンティティを削除します
    /// </summary>
    /// <param name="id">ID</param>
    /// <returns>タスク</returns>
    Task DeleteByIdAsync(int id);

    /// <summary>
    /// 条件に一致するエンティティが存在するかを確認します
    /// </summary>
    /// <param name="predicate">条件式</param>
    /// <returns>存在するかどうか</returns>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// 条件に一致するエンティティの数をカウントします
    /// </summary>
    /// <param name="predicate">条件式</param>
    /// <returns>カウント数</returns>
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);
}