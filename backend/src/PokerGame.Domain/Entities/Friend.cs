using SqlSugar;
using PokerGame.Domain.Enums;

namespace PokerGame.Domain.Entities;

/// <summary>
/// 好友关系实体
/// </summary>
[SugarTable("friends")]
public class Friend
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long UserId { get; set; }

    /// <summary>
    /// 好友ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long FriendId { get; set; }

    /// <summary>
    /// 好友状态
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public FriendStatus Status { get; set; } = FriendStatus.Pending;

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
