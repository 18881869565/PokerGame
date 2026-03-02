using PokerGame.Domain.Enums;

namespace PokerGame.Domain.GameLogic;

/// <summary>
/// 游戏状态（运行时，内存中保存）
/// </summary>
public class GameState
{
    /// <summary>
    /// 房间ID
    /// </summary>
    public long RoomId { get; set; }

    /// <summary>
    /// 游戏ID（数据库中的ID，游戏开始后生成）
    /// </summary>
    public long? GameId { get; set; }

    /// <summary>
    /// 当前游戏阶段
    /// </summary>
    public GamePhase Phase { get; set; } = GamePhase.Waiting;

    /// <summary>
    /// 牌组
    /// </summary>
    public Deck Deck { get; set; } = null!;

    /// <summary>
    /// 公共牌
    /// </summary>
    public List<Card> CommunityCards { get; set; } = new();

    /// <summary>
    /// 参与游戏的玩家
    /// </summary>
    public List<GamePlayer> Players { get; set; } = new();

    /// <summary>
    /// 底池
    /// </summary>
    public long Pot { get; set; }

    /// <summary>
    /// 当前阶段最高下注
    /// </summary>
    public long CurrentHighestBet { get; set; }

    /// <summary>
    /// 当前需要跟注的金额
    /// </summary>
    public long CurrentCallAmount => CurrentHighestBet;

    /// <summary>
    /// 当前操作玩家索引
    /// </summary>
    public int CurrentPlayerIndex { get; set; } = -1;

    /// <summary>
    /// 庄家位索引
    /// </summary>
    public int DealerIndex { get; set; }

    /// <summary>
    /// 小盲注
    /// </summary>
    public int SmallBlind { get; set; } = 10;

    /// <summary>
    /// 大盲注
    /// </summary>
    public int BigBlind { get; set; } = 20;

    /// <summary>
    /// 最近操作的玩家
    /// </summary>
    public long? LastActionPlayerId { get; set; }

    /// <summary>
    /// 本阶段第一个操作的玩家索引
    /// </summary>
    public int FirstActorIndex { get; set; }

    /// <summary>
    /// 游戏开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 是否是多人底池（用于判断是否需要摊牌）
    /// </summary>
    public bool IsMultiWayPot => GetActivePlayers().Count > 1;

    /// <summary>
    /// 获取当前操作玩家
    /// </summary>
    public GamePlayer? CurrentPlayer => CurrentPlayerIndex >= 0 && CurrentPlayerIndex < Players.Count
        ? Players[CurrentPlayerIndex]
        : null;

    /// <summary>
    /// 获取还在游戏中的玩家（未弃牌）
    /// </summary>
    public List<GamePlayer> GetActivePlayers() => Players.Where(p => p.IsInGame).ToList();

    /// <summary>
    /// 获取可以操作的玩家
    /// </summary>
    public List<GamePlayer> GetActingPlayers() => Players.Where(p => p.CanAct && p.Chips > 0).ToList();

    /// <summary>
    /// 根据用户ID获取玩家
    /// </summary>
    public GamePlayer? GetPlayer(long userId) => Players.FirstOrDefault(p => p.UserId == userId);

    /// <summary>
    /// 根据座位索引获取玩家
    /// </summary>
    public GamePlayer? GetPlayerBySeat(int seatIndex) => Players.FirstOrDefault(p => p.SeatIndex == seatIndex);

    /// <summary>
    /// 获取下一个操作玩家索引
    /// </summary>
    public int GetNextPlayerIndex(int fromIndex)
    {
        if (Players.Count == 0) return -1;

        for (int i = 1; i <= Players.Count; i++)
        {
            var nextIndex = (fromIndex + i) % Players.Count;
            var player = Players[nextIndex];
            // 跳过已弃牌和已全押的玩家
            if (player.Status != PlayerStatus.Folded && player.Status != PlayerStatus.AllIn)
            {
                return nextIndex;
            }
        }

        return -1;
    }

    /// <summary>
    /// 是否所有人都已操作完毕（本轮下注结束）
    /// </summary>
    public bool IsBettingRoundComplete()
    {
        var actingPlayers = GetActingPlayers();

        // 如果只有一个或没有可以操作的玩家，结束本轮
        if (actingPlayers.Count <= 1) return true;

        // 检查所有可以操作的玩家是否都已下注到当前最高注
        foreach (var player in actingPlayers)
        {
            // 如果有玩家还没达到当前最高注，且没有全押，则还没结束
            if (player.CurrentBet < CurrentHighestBet && player.Status != PlayerStatus.AllIn)
            {
                return false;
            }

            // 如果有玩家还没有操作过
            if (player.LastAction == null && player.Status != PlayerStatus.AllIn)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 重置到等待状态
    /// </summary>
    public void Reset()
    {
        Phase = GamePhase.Waiting;
        CommunityCards.Clear();
        Pot = 0;
        CurrentHighestBet = 0;
        CurrentPlayerIndex = -1;
        LastActionPlayerId = null;

        foreach (var player in Players)
        {
            player.ResetForNewRound();
        }
    }
}
