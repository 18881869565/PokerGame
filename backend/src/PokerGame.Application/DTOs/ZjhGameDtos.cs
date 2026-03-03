using PokerGame.Domain.Enums;

namespace PokerGame.Application.DTOs;

/// <summary>
/// 扎金花游戏状态 DTO（用于 SignalR 推送）
/// </summary>
public class ZjhGameStateDto
{
    /// <summary>
    /// 游戏阶段
    /// </summary>
    public ZjhGamePhase Phase { get; set; }

    /// <summary>
    /// 底池
    /// </summary>
    public long Pot { get; set; }

    /// <summary>
    /// 当前基础下注额
    /// </summary>
    public long BetAmount { get; set; }

    /// <summary>
    /// 底注金额
    /// </summary>
    public long AnteAmount { get; set; }

    /// <summary>
    /// 当前轮数
    /// </summary>
    public int RoundCount { get; set; }

    /// <summary>
    /// 最大轮数
    /// </summary>
    public int MaxRounds { get; set; }

    /// <summary>
    /// 当前操作玩家用户ID
    /// </summary>
    public long? CurrentPlayerId { get; set; }

    /// <summary>
    /// 玩家列表
    /// </summary>
    public List<ZjhPlayerDto> Players { get; set; } = new();

    /// <summary>
    /// 赢家ID
    /// </summary>
    public long? WinnerId { get; set; }
}

/// <summary>
/// 扎金花玩家 DTO
/// </summary>
public class ZjhPlayerDto
{
    public long UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public int SeatIndex { get; set; }
    public long Chips { get; set; }
    public long TotalBet { get; set; }
    public bool HasLooked { get; set; }
    public bool IsFolded { get; set; }
    public bool IsOut { get; set; }
    public bool IsCompareLose { get; set; }
    public ZjhAction? LastAction { get; set; }

    /// <summary>
    /// 手牌（仅自己可见或游戏结束时可见）
    /// </summary>
    public List<CardDto>? Hand { get; set; }

    /// <summary>
    /// 是否还在游戏中
    /// </summary>
    public bool IsInGame => !IsFolded && !IsOut && !IsCompareLose;

    /// <summary>
    /// 下注倍率（看牌后翻倍）
    /// </summary>
    public int BetMultiplier => HasLooked ? 2 : 1;

    /// <summary>
    /// 是否在线
    /// </summary>
    public bool IsOnline { get; set; } = true;
}

/// <summary>
/// 扎金花游戏结果 DTO
/// </summary>
public class ZjhGameResultDto
{
    /// <summary>
    /// 赢家ID
    /// </summary>
    public long WinnerId { get; set; }

    /// <summary>
    /// 赢家昵称
    /// </summary>
    public string WinnerNickname { get; set; } = string.Empty;

    /// <summary>
    /// 底池金额
    /// </summary>
    public long Pot { get; set; }

    /// <summary>
    /// 赢家牌型
    /// </summary>
    public string? HandDescription { get; set; }

    /// <summary>
    /// 所有玩家的手牌（摊牌后可见）
    /// </summary>
    public List<ZjhPlayerHandResultDto> PlayerHands { get; set; } = new();
}

/// <summary>
/// 扎金花玩家手牌结果 DTO
/// </summary>
public class ZjhPlayerHandResultDto
{
    public long UserId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public List<CardDto> Hand { get; set; } = new();
    public string? HandDescription { get; set; }
    public bool IsWinner { get; set; }
}

/// <summary>
/// 扎金花玩家可用操作 DTO
/// </summary>
public class ZjhAvailableActionsDto
{
    public List<ZjhAction> Actions { get; set; } = new();
    public long MinBetAmount { get; set; }
    public long CompareCost { get; set; }
}

/// <summary>
/// 扎金花比牌结果 DTO（用于通知输家看牌）
/// </summary>
public class ZjhCompareResultDto
{
    /// <summary>
    /// 输家ID
    /// </summary>
    public long LoserId { get; set; }

    /// <summary>
    /// 输家是否没看过牌
    /// </summary>
    public bool LoserHasNotLooked { get; set; }

    /// <summary>
    /// 输家的手牌信息（仅在没看过牌时填充）
    /// </summary>
    public ZjhPlayerHandResultDto? LoserHand { get; set; }
}
