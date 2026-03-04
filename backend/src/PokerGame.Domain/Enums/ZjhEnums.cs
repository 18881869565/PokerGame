namespace PokerGame.Domain.Enums;

/// <summary>
/// 扎金花游戏阶段
/// </summary>
public enum ZjhGamePhase
{
    /// <summary>
    /// 等待开始
    /// </summary>
    Waiting = 0,

    /// <summary>
    /// 发牌中
    /// </summary>
    Dealing = 1,

    /// <summary>
    /// 下注阶段
    /// </summary>
    Betting = 2,

    /// <summary>
    /// 比牌阶段
    /// </summary>
    Comparing = 3,

    /// <summary>
    /// 游戏结束
    /// </summary>
    Finished = 4
}

/// <summary>
/// 扎金花牌型（从大到小）
/// </summary>
public enum ZjhHandRank
{
    /// <summary>
    /// 高牌/散牌
    /// </summary>
    HighCard = 1,

    /// <summary>
    /// 对子
    /// </summary>
    Pair = 2,

    /// <summary>
    /// 顺子
    /// </summary>
    Straight = 3,

    /// <summary>
    /// 金花（同花）
    /// </summary>
    Flush = 4,

    /// <summary>
    /// 顺金（同花顺）
    /// </summary>
    StraightFlush = 5,

    /// <summary>
    /// 豹子（三条）
    /// </summary>
    ThreeOfAKind = 6
}

/// <summary>
/// 扎金花玩家操作
/// </summary>
public enum ZjhAction
{
    /// <summary>
    /// 看牌
    /// </summary>
    Look = 1,

    /// <summary>
    /// 闷牌下注
    /// </summary>
    BetBlind = 2,

    /// <summary>
    /// 看牌下注
    /// </summary>
    BetLook = 3,

    /// <summary>
    /// 比牌
    /// </summary>
    Compare = 4,

    /// <summary>
    /// 弃牌
    /// </summary>
    Fold = 5,

    /// <summary>
    /// 全押
    /// </summary>
    AllIn = 6
}
