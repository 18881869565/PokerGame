using SqlSugar;

namespace PokerGame.Domain.Entities;

/// <summary>
/// 房间玩家实体
/// </summary>
[SugarTable("room_players")]
public class RoomPlayer
{
    /// <summary>
    /// 主键ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 房间ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long RoomId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long UserId { get; set; }

    /// <summary>
    /// 座位号
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public int SeatIndex { get; set; }

    /// <summary>
    /// 带入筹码
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long Chips { get; set; }

    /// <summary>
    /// 是否准备
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool IsReady { get; set; }

    /// <summary>
    /// 是否在线
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public bool IsOnline { get; set; } = true;
}
