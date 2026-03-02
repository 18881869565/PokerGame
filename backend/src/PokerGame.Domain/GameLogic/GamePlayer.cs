using PokerGame.Domain.Enums;

namespace PokerGame.Domain.GameLogic;

/// <summary>
/// 游戏中的玩家状态（运行时）
/// </summary>
public class GamePlayer
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
    /// 手牌（底牌，2张）
    /// </summary>
    public List<Card> HoleCards { get; set; } = new();

    /// <summary>
    /// 本轮已下注金额
    /// </summary>
    public long CurrentBet { get; set; }

    /// <summary>
    /// 本局总下注金额
    /// </summary>
    public long TotalBet { get; set; }

    /// <summary>
    /// 玩家状态
    /// </summary>
    public PlayerStatus Status { get; set; } = PlayerStatus.Waiting;

    /// <summary>
    /// 最后一次操作
    /// </summary>
    public PlayerAction? LastAction { get; set; }

    /// <summary>
    /// 是否是庄家位（Button）
    /// </summary>
    public bool IsDealer { get; set; }

    /// <summary>
    /// 是否是小盲位
    /// </summary>
    public bool IsSmallBlind { get; set; }

    /// <summary>
    /// 是否是大盲位
    /// </summary>
    public bool IsBigBlind { get; set; }

    /// <summary>
    /// 是否还在游戏中（未弃牌）
    /// </summary>
    public bool IsInGame => Status != PlayerStatus.Folded;

    /// <summary>
    /// 是否可以操作
    /// </summary>
    public bool CanAct => Status == PlayerStatus.Waiting || Status == PlayerStatus.MyTurn;

    /// <summary>
    /// 重置新一轮状态
    /// </summary>
    public void ResetForNewRound()
    {
        HoleCards.Clear();
        CurrentBet = 0;
        TotalBet = 0;
        Status = PlayerStatus.Waiting;
        LastAction = null;
        IsDealer = false;
        IsSmallBlind = false;
        IsBigBlind = false;
    }

    /// <summary>
    /// 重置本阶段下注
    /// </summary>
    public void ResetBetForNewPhase()
    {
        CurrentBet = 0;
    }
}
