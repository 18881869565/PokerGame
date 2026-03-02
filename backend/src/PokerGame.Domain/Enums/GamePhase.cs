namespace PokerGame.Domain.Enums;

/// <summary>
/// 游戏阶段
/// </summary>
public enum GamePhase
{
    /// <summary>
    /// 等待玩家
    /// </summary>
    Waiting = 0,

    /// <summary>
    /// 游戏开始中
    /// </summary>
    Starting = 1,

    /// <summary>
    /// 发底牌阶段（每人2张）
    /// </summary>
    PreFlop = 2,

    /// <summary>
    /// 翻牌阶段（3张公共牌）
    /// </summary>
    Flop = 3,

    /// <summary>
    /// 转牌阶段（1张公共牌）
    /// </summary>
    Turn = 4,

    /// <summary>
    /// 河牌阶段（1张公共牌）
    /// </summary>
    River = 5,

    /// <summary>
    /// 摊牌阶段
    /// </summary>
    Showdown = 6,

    /// <summary>
    /// 本局结束
    /// </summary>
    Finished = 7
}
