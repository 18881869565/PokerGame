using PokerGame.Domain.Enums;

namespace PokerGame.Domain.GameLogic;

/// <summary>
/// 扎金花游戏玩家状态（运行时）
/// </summary>
public class ZjhPlayer
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 昵称
    /// </summary>
    public string Nickname { get; set; } = string.Empty;

    /// <summary>
    /// 座位号（0-8）
    /// </summary>
    public int SeatIndex { get; set; }

    /// <summary>
    /// 当前筹码
    /// </summary>
    public long Chips { get; set; }

    /// <summary>
    /// 手牌（3张）
    /// </summary>
    public List<Card> Hand { get; set; } = new();

    /// <summary>
    /// 本局总下注金额
    /// </summary>
    public long TotalBet { get; set; }

    /// <summary>
    /// 是否已看牌
    /// </summary>
    public bool HasLooked { get; set; }

    /// <summary>
    /// 是否已弃牌
    /// </summary>
    public bool IsFolded { get; set; }

    /// <summary>
    /// 是否已出局（筹码耗尽）
    /// </summary>
    public bool IsOut { get; set; }

    /// <summary>
    /// 是否已比牌输掉
    /// </summary>
    public bool IsCompareLose { get; set; }

    /// <summary>
    /// 最后一次操作
    /// </summary>
    public ZjhAction? LastAction { get; set; }

    /// <summary>
    /// 是否还在游戏中（未弃牌、未出局、未比牌输掉）
    /// </summary>
    public bool IsInGame => !IsFolded && !IsOut && !IsCompareLose;

    /// <summary>
    /// 获取当前下注倍率（看牌后下注翻倍）
    /// </summary>
    public int BetMultiplier => HasLooked ? 2 : 1;

    /// <summary>
    /// 重置新一轮状态
    /// </summary>
    public void ResetForNewRound()
    {
        Hand.Clear();
        TotalBet = 0;
        HasLooked = false;
        IsFolded = false;
        IsCompareLose = false;
        LastAction = null;
    }

    /// <summary>
    /// 扣除筹码
    /// </summary>
    public bool DeductChips(long amount)
    {
        if (Chips < amount) return false;
        Chips -= amount;
        TotalBet += amount;
        return true;
    }
}
