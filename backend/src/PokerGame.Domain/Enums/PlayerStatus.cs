namespace PokerGame.Domain.Enums;

/// <summary>
/// 玩家状态
/// </summary>
public enum PlayerStatus
{
    /// <summary>
    /// 等待轮次
    /// </summary>
    Waiting = 0,

    /// <summary>
    /// 轮到我操作
    /// </summary>
    MyTurn = 1,

    /// <summary>
    /// 已弃牌
    /// </summary>
    Folded = 2,

    /// <summary>
    /// 已全押
    /// </summary>
    AllIn = 3,

    /// <summary>
    /// 筹码不足
    /// </summary>
    OutOfChips = 4
}
