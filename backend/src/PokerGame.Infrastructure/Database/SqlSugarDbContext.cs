using Microsoft.Extensions.Configuration;
using SqlSugar;
using PokerGame.Domain.Entities;

namespace PokerGame.Infrastructure.Database;

/// <summary>
/// SqlSugar 数据库上下文
/// </summary>
public class SqlSugarDbContext
{
    private readonly ISqlSugarClient _db;

    public SqlSugarDbContext(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        Console.WriteLine($"[DEBUG] ConnectionString: {connectionString ?? "NULL"}");

        if (string.IsNullOrEmpty(connectionString))
        {
            // 尝试直接从配置中读取
            var connStrObj = configuration["ConnectionStrings:DefaultConnection"];
            Console.WriteLine($"[DEBUG] Direct read: {connStrObj ?? "NULL"}");

            if (string.IsNullOrEmpty(connStrObj))
            {
                throw new InvalidOperationException(
                    "数据库连接字符串未配置。请检查 appsettings.json 中的 ConnectionStrings:DefaultConnection 配置项。");
            }
            connectionString = connStrObj;
        }

        _db = new SqlSugarScope(new ConnectionConfig
        {
            ConnectionString = connectionString,
            DbType = DbType.MySql,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        },
        db =>
        {
#if DEBUG
            db.Aop.OnLogExecuting = (sql, pars) =>
            {
                Console.WriteLine($"[SQL] {sql}");
                if (pars != null && pars.Length > 0)
                {
                    Console.WriteLine($"[Parameters] {string.Join(", ", pars.Select(p => $"{p.ParameterName}={p.Value}"))}");
                }
            };
#endif
        });
    }

    /// <summary>
    /// 获取 SqlSugar 客户端
    /// </summary>
    public ISqlSugarClient Db => _db;

    /// <summary>
    /// 初始化数据库（创建数据库和表）
    /// </summary>
    public void InitializeDatabase()
    {
        Console.WriteLine("开始初始化数据库...");

        try
        {
            // 创建数据库（如果不存在）
            _db.DbMaintenance.CreateDatabase();
            Console.WriteLine("数据库创建/连接成功");

            // 创建表（如果不存在）
            var types = new Type[]
            {
                typeof(User),
                typeof(Friend),
                typeof(Room),
                typeof(RoomPlayer),
                typeof(Game)
            };

            foreach (var type in types)
            {
                var tableName = _db.EntityMaintenance.GetEntityInfo(type).DbTableName;
                Console.WriteLine($"检查表: {tableName}");

                if (!_db.DbMaintenance.IsAnyTable(tableName))
                {
                    _db.CodeFirst.InitTables(type);
                    Console.WriteLine($"创建表成功: {tableName}");
                }
                else
                {
                    Console.WriteLine($"表已存在: {tableName}");
                }
            }

            Console.WriteLine("数据库初始化完成!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"数据库初始化异常: {ex.Message}");
            Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// 获取实体仓储
    /// </summary>
    public ISugarQueryable<T> Queryable<T>() where T : class, new()
    {
        return _db.Queryable<T>();
    }

    /// <summary>
    /// 插入实体
    /// </summary>
    public async Task<int> InsertAsync<T>(T entity) where T : class, new()
    {
        return await _db.Insertable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 更新实体
    /// </summary>
    public async Task<int> UpdateAsync<T>(T entity) where T : class, new()
    {
        return await _db.Updateable(entity).ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除实体
    /// </summary>
    public async Task<int> DeleteAsync<T>(T entity) where T : class, new()
    {
        return await _db.Deleteable(entity).ExecuteCommandAsync();
    }
}
