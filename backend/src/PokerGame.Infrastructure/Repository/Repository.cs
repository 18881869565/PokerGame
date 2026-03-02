using SqlSugar;

namespace PokerGame.Infrastructure.Repository;

/// <summary>
/// 通用仓储实现
/// </summary>
public class Repository<T> : IRepository<T> where T : class, new()
{
    private readonly ISqlSugarClient _db;

    public Repository(ISqlSugarClient db)
    {
        _db = db;
    }

    /// <summary>
    /// 获取 SqlSugar 客户端
    /// </summary>
    public ISqlSugarClient Db => _db;

    /// <summary>
    /// 根据ID获取实体
    /// </summary>
    public async Task<T?> GetByIdAsync(long id)
    {
        return await _db.Queryable<T>()
            .Where("Id = @id", new { id })
            .FirstAsync();
    }

    /// <summary>
    /// 获取所有实体
    /// </summary>
    public async Task<List<T>> GetAllAsync()
    {
        return await _db.Queryable<T>().ToListAsync();
    }

    /// <summary>
    /// 添加实体
    /// </summary>
    public async Task<int> AddAsync(T entity)
    {
        return await _db.Insertable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 更新实体
    /// </summary>
    public async Task<int> UpdateAsync(T entity)
    {
        return await _db.Updateable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除实体
    /// </summary>
    public async Task<int> DeleteAsync(long id)
    {
        return await _db.Deleteable<T>()
            .Where("Id = @id", new { id })
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 条件查询
    /// </summary>
    public async Task<List<T>> GetListAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
    {
        return await _db.Queryable<T>().Where(predicate).ToListAsync();
    }

    /// <summary>
    /// 获取单个实体
    /// </summary>
    public async Task<T?> FirstOrDefaultAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
    {
        return await _db.Queryable<T>().FirstAsync(predicate);
    }

    /// <summary>
    /// 判断是否存在
    /// </summary>
    public async Task<bool> AnyAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
    {
        return await _db.Queryable<T>().AnyAsync(predicate);
    }

    /// <summary>
    /// 添加实体并返回自增ID
    /// </summary>
    public async Task<long> InsertReturnIdentityAsync(T entity)
    {
        return await _db.Insertable(entity).ExecuteReturnBigIdentityAsync();
    }

    /// <summary>
    /// 统计数量
    /// </summary>
    public async Task<int> CountAsync(System.Linq.Expressions.Expression<Func<T, bool>> predicate)
    {
        return await _db.Queryable<T>().Where(predicate).CountAsync();
    }
}
