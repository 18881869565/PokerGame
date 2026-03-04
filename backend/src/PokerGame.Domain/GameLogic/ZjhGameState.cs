using PokerGame.Domain.Enums;

namespace PokerGame.Domain.GameLogic;

/// <summary>
/// 扎金花游戏状态（运行时，内存中保存）
/// </summary>
public class ZjhGameState
{
    /// <summary>
    /// 房间ID
    /// </summary>
    public long RoomId { get; set; }

    /// <summary>
    /// 游戏ID（数据库中的ID）
    /// </summary>
    public long? GameId { get; set; }

    /// <summary>
    /// 当前游戏阶段
    /// </summary>
    public ZjhGamePhase Phase { get; set; } = ZjhGamePhase.Waiting;

    /// <summary>
    /// 牌组
    /// </summary>
    public Deck Deck { get; set; } = null!;

    /// <summary>
    /// 底池
    /// </summary>
    public long Pot { get; set; }

    /// <summary>
    /// 底注金额（每局开始时每人支付）
    /// </summary>
    public long AnteAmount { get; set; } = 1;

    /// <summary>
    /// 当前基础下注额
    /// </summary>
    public long BetAmount { get; set; } = 1;

    /// <summary>
    /// 当前轮数
    /// </summary>
    public int RoundCount { get; set; }

    /// <summary>
    /// 最大轮数限制
    /// </summary>
    public int MaxRounds { get; set; } = 10;

    /// <summary>
    /// 参与游戏的玩家
    /// </summary>
    public List<ZjhPlayer> Players { get; set; } = new();

    /// <summary>
    /// 当前操作玩家索引
    /// </summary>
    public int CurrentPlayerIndex { get; set; } = -1;

    /// <summary>
    /// 赢家ID
    /// </summary>
    public long? WinnerId { get; set; }

    /// <summary>
    /// 赢家的牌
    /// </summary>
    public List<Card>? WinnerHand { get; set; }

    /// <summary>
    /// 获取当前操作的玩家
    /// </summary>
    public ZjhPlayer? CurrentPlayer => CurrentPlayerIndex >= 0 && CurrentPlayerIndex < Players.Count
        ? Players[CurrentPlayerIndex]
        : null;

    /// <summary>
    /// 获取存活的玩家（未弃牌、未出局、未比牌输）
    /// </summary>
    public List<ZjhPlayer> GetActivePlayers() => Players.Where(p => !p.IsFolded && !p.IsOut && !p.IsCompareLose).ToList();

    /// <summary>
    /// 获取可以操作的玩家（存活且有筹码）
    /// </summary>
    public List<ZjhPlayer> GetActingPlayers() => Players.Where(p => !p.IsFolded && !p.IsOut && p.Chips > 0).ToList();

    /// <summary>
    /// 获取下一个玩家索引
    /// </summary>
    public int GetNextPlayerIndex(int fromIndex)
    {
        if (Players.Count == 0) return -1;

        for (int i = 1; i <= Players.Count; i++)
        {
            var nextIndex = (fromIndex + i) % Players.Count;
            var player = Players[nextIndex];
            // 跳过已弃牌、已出局、比牌输掉的玩家
            if (!player.IsFolded && !player.IsOut && !player.IsCompareLose)
            {
                return nextIndex;
            }
        }

        return -1;
    }

    /// <summary>
    /// 获取指定玩家
    /// </summary>
    public ZjhPlayer? GetPlayer(long userId) => Players.FirstOrDefault(p => p.UserId == userId);
}
