namespace PokerGame.Domain.Enums;

/// <summary>
/// 房间状态
/// </summary>
public enum RoomStatus
{
    /// <summary>
    /// 等待中
    /// </summary>
    Waiting = 0,

    /// <summary>
    /// 进行中
    /// </summary>
    Playing = 1,

    /// <summary>
    /// 已结束
    /// </summary>
    Finished = 2
}
