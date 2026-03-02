namespace PokerGame.Domain.GameLogic;

/// <summary>
/// 牌型等级（从高到低）
/// </summary>
public enum HandRank
{
    /// <summary>
    /// 高牌
    /// </summary>
    HighCard = 1,

    /// <summary>
    /// 一对
    /// </summary>
    OnePair = 2,

    /// <summary>
    /// 两对
    /// </summary>
    TwoPair = 3,

    /// <summary>
    /// 三条
    /// </summary>
    ThreeOfAKind = 4,

    /// <summary>
    /// 顺子
    /// </summary>
    Straight = 5,

    /// <summary>
    /// 同花
    /// </summary>
    Flush = 6,

    /// <summary>
    /// 葫芦
    /// </summary>
    FullHouse = 7,

    /// <summary>
    /// 四条
    /// </summary>
    FourOfAKind = 8,

    /// <summary>
    /// 同花顺
    /// </summary>
    StraightFlush = 9,

    /// <summary>
    /// 皇家同花顺
    /// </summary>
    RoyalFlush = 10
}

/// <summary>
/// 手牌评估结果
/// </summary>
public class HandEvaluation : IComparable<HandEvaluation>
{
    /// <summary>
    /// 牌型等级
    /// </summary>
    public HandRank Rank { get; set; }

    /// <summary>
    /// 用于比较高低的牌值（从高到低）
    /// </summary>
    public List<int> Kickers { get; set; } = new();

    /// <summary>
    /// 描述
    /// </summary>
    public string Description => GetDescription();

    public int CompareTo(HandEvaluation? other)
    {
        if (other == null) return 1;

        // 先比较牌型等级
        int rankCompare = Rank.CompareTo(other.Rank);
        if (rankCompare != 0) return rankCompare;

        // 牌型相同，比较踢脚牌
        for (int i = 0; i < Math.Min(Kickers.Count, other.Kickers.Count); i++)
        {
            int kickerCompare = Kickers[i].CompareTo(other.Kickers[i]);
            if (kickerCompare != 0) return kickerCompare;
        }

        return 0;
    }

    private string GetDescription()
    {
        return Rank switch
        {
            HandRank.RoyalFlush => "皇家同花顺",
            HandRank.StraightFlush => "同花顺",
            HandRank.FourOfAKind => "四条",
            HandRank.FullHouse => "葫芦",
            HandRank.Flush => "同花",
            HandRank.Straight => "顺子",
            HandRank.ThreeOfAKind => "三条",
            HandRank.TwoPair => "两对",
            HandRank.OnePair => "一对",
            HandRank.HighCard => "高牌",
            _ => "未知牌型"
        };
    }

    public override string ToString() => $"{Description} [{string.Join(", ", Kickers)}]";
}
