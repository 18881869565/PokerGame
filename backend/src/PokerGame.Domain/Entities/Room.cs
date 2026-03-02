using SqlSugar;
using PokerGame.Domain.Enums;

namespace PokerGame.Domain.Entities;

/// <summary>
/// 房间实体
/// </summary>
[SugarTable("rooms")]
public class Room
{
    /// <summary>
    /// 房间ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 6位房间号
    /// </summary>
    [SugarColumn(Length = 6, IsNullable = false)]
    public string RoomCode { get; set; } = string.Empty;

    /// <summary>
    /// 房主ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long OwnerId { get; set; }

    /// <summary>
    /// 最大玩家数
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int MaxPlayers { get; set; } = 9;

    /// <summary>
    /// 小盲注
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int SmallBlind { get; set; } = 10;

    /// <summary>
    /// 大盲注
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int BigBlind { get; set; } = 20;

    /// <summary>
    /// 房间状态
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public RoomStatus Status { get; set; } = RoomStatus.Waiting;

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
