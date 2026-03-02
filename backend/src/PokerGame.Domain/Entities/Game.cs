using SqlSugar;

namespace PokerGame.Domain.Entities;

/// <summary>
/// 游戏对局实体
/// </summary>
[SugarTable("games")]
public class Game
{
    /// <summary>
    /// 对局ID
    /// </summary>
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public long Id { get; set; }

    /// <summary>
    /// 房间ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long RoomId { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 赢家ID
    /// </summary>
    [SugarColumn(IsNullable = true)]
    public long? WinnerId { get; set; }

    /// <summary>
    /// 底池
    /// </summary>
    [SugarColumn(IsNullable = false)]
    public long Pot { get; set; }
}
