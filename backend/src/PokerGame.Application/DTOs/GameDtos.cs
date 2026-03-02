using PokerGame.Domain.Enums;
using PokerGame.Domain.GameLogic;

namespace PokerGame.Application.DTOs;

/// <summary>
/// 游戏状态 DTO（用于 SignalR 推送）
/// </summary>
public class GameStateDto
{
    /// <summary>
    /// 游戏阶段
    /// </summary>
    public GamePhase Phase { get; set; }

    /// <summary>
    /// 公共牌
    /// </summary>
    public List<CardDto> CommunityCards { get; set; } = new();

    /// <summary>
    /// 玩家列表（不含底牌信息）
    /// </summary>
    public List<GamePlayerDto> Players { get; set; } = new();

    /// <summary>
    /// 底池
    /// </summary>
    public long Pot { get; set; }

    /// <summary>
    /// 当前最高下注
    /// </summary>
    public long CurrentHighestBet { get; set; }

    /// <summary>
    /// 当前操作玩家用户ID
    /// </summary>
    public long? CurrentPlayerId { get; set; }

    /// <summary>
    /// 庄家位用户ID
    /// </summary>
    public long DealerId { get; set; }

    /// <summary>
    /// 小盲注
    /// </summary>
    public int SmallBlind { get; set; }

    /// <summary>
    /// 大盲注
    /// </summary>
    public int BigBlind { get; set; }
}

/// <summary>
/// 游戏玩家 DTO
/// </summary>
public class GamePlayerDto
{
    public long UserId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public int SeatIndex { get; set; }
    public long Chips { get; set; }
    public long CurrentBet { get; set; }
    public PlayerStatus Status { get; set; }
    public PlayerAction? LastAction { get; set; }
    public bool IsDealer { get; set; }
    public bool IsSmallBlind { get; set; }
    public bool IsBigBlind { get; set; }
    public List<CardDto>? HoleCards { get; set; } // 仅自己的牌可见
}

/// <summary>
/// 扑克牌 DTO
/// </summary>
public class CardDto
{
    public int Suit { get; set; }
    public int Rank { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    public static CardDto FromCard(Card card)
    {
        return new CardDto
        {
            Suit = (int)card.Suit,
            Rank = (int)card.Rank,
            DisplayName = card.DisplayName
        };
    }
}

/// <summary>
/// 玩家操作请求
/// </summary>
public class PlayerActionDto
{
    public PlayerAction Action { get; set; }
    public long Amount { get; set; } // 用于 Raise
}

/// <summary>
/// 游戏结果 DTO
/// </summary>
public class GameResultDto
{
    public List<long> WinnerIds { get; set; } = new();
    public long Pot { get; set; }
    public List<PlayerHandResultDto> PlayerHands { get; set; } = new();
}

/// <summary>
/// 玩家手牌结果 DTO
/// </summary>
public class PlayerHandResultDto
{
    public long UserId { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public List<CardDto> HoleCards { get; set; } = new();
    public string? HandDescription { get; set; }
    public long ChipsWon { get; set; }
}
