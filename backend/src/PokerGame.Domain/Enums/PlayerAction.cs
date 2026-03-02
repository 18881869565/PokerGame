namespace PokerGame.Domain.Enums;

/// <summary>
/// 玩家操作类型
/// </summary>
public enum PlayerAction
{
    /// <summary>
    /// 弃牌
    /// </summary>
    Fold = 0,

    /// <summary>
    /// 过牌
    /// </summary>
    Check = 1,

    /// <summary>
    /// 跟注
    /// </summary>
    Call = 2,

    /// <summary>
    /// 加注
    /// </summary>
    Raise = 3,

    /// <summary>
    /// 全押
    /// </summary>
    AllIn = 4
}
