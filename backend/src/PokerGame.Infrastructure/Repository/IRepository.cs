using SqlSugar;

namespace PokerGame.Infrastructure.Repository;

/// <summary>
/// 通用仓储接口
/// </summary>
public interface IRepository<T> where T : class, new()
{
    /// <summary>
    /// 获取 SqlSugar 客户端（用于复杂查询）
    /// </summary>
    ISqlSugarClient Db { get; }

    /// <summary>
    /// 根据ID获取实体
    /// </summary>
    Task<T?> GetByIdAsync(long id);

    /// <summary>
    /// 获取所有实体
    /// </summary>
    Task<List<T>> GetAllAsync();

    /// <summary>
    /// 添加实体
    /// </summary>
    Task<int> AddAsync(T entity);

    /// <summary>
    /// 更新实体
    /// </summary>
    Task<int> UpdateAsync(T entity);

    /// <summary>
    /// 删除实体
    /// </summary>
    Task<int> DeleteAsync(long id);

    /// <summary>
    /// 条件查询
    /// </summary>
    Task<List<T>> GetListAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate);

    /// <summary>
    /// 获取单个实体
    /// </summary>
    Task<T?> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate);

    /// <summary>
    /// 判断是否存在
    /// </summary>
    Task<bool> AnyAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate);

    /// <summary>
    /// 添加实体并返回自增ID
    /// </summary>
    Task<long> InsertReturnIdentityAsync(T entity);

    /// <summary>
    /// 统计数量
    /// </summary>
    Task<int> CountAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate);
}
