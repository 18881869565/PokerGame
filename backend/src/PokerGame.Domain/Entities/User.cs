using SqlSugar;
using PokerGame.Domain.Enums;

namespace PokerGame.Domain.Entities;

/// <summary>
/// 用户实体
/// </summary>
[SugarTable("users")]
public class User
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码哈希
    /// </summary>
    [SugarColumn(Length = 255, IsNullable = false)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// 昵称
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    public string Nickname { get; set; } = string.Empty;

    /// <summary>
    /// 头像URL
    /// </summary>
    [SugarColumn(Length = 255, IsNullable = true)]
    public string? Avatar { get; set; }

    /// <summary>
    /// 虚拟筹码
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long Chips { get; set; } = 10000;

    /// <summary>
    /// 上次每日赠送时间
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public DateTime? DailyGiftAt { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 更新时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
