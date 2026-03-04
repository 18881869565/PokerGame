using PokerGame.Domain.Enums;

namespace PokerGame.Domain.GameLogic;

/// <summary>
/// 扎金花牌型评估器
/// </summary>
public class ZjhHandEvaluator
{
    /// <summary>
    /// 评估三张牌的牌型
    /// </summary>
    public (ZjhHandRank Rank, int Score) Evaluate(List<Card> cards)
    {
        if (cards == null || cards.Count != 3)
            return (ZjhHandRank.HighCard, 0);

        var rank = DetermineHandRank(cards);
        var score = CalculateScore(cards, rank);

        return (rank, score);
    }

    /// <summary>
    /// 比较两手牌的大小
    /// 返回值：正数表示 hand1 大，负数表示 hand2 大，0 表示相等
    /// </summary>
    public int Compare(List<Card> hand1, List<Card> hand2)
    {
        var eval1 = Evaluate(hand1);
        var eval2 = Evaluate(hand2);

        // 先比较牌型
        if (eval1.Rank != eval2.Rank)
        {
            return eval1.Rank > eval2.Rank ? 1 : -1;
        }

        // 牌型相同，比较分数
        return eval1.Score.CompareTo(eval2.Score);
    }

    /// <summary>
    /// 确定牌型
    /// </summary>
    private ZjhHandRank DetermineHandRank(List<Card> cards)
    {
        var sortedCards = cards.OrderBy(c => c.Rank).ToList();

        bool isFlush = IsFlush(cards);
        bool isStraight = IsStraight(sortedCards);
        bool isThreeOfAKind = IsThreeOfAKind(sortedCards);
        bool isPair = IsPair(sortedCards);

        // 豹子（三条）
        if (isThreeOfAKind)
            return ZjhHandRank.ThreeOfAKind;

        // 顺金（同花顺）
        if (isFlush && isStraight)
            return ZjhHandRank.StraightFlush;

        // 金花（同花）
        if (isFlush)
            return ZjhHandRank.Flush;

        // 顺子
        if (isStraight)
            return ZjhHandRank.Straight;

        // 对子
        if (isPair)
            return ZjhHandRank.Pair;

        // 高牌
        return ZjhHandRank.HighCard;
    }

    /// <summary>
    /// 计算分数（用于同牌型比较）
    /// 分数 = 点数权重 * 10000 + 花色权重
    /// </summary>
    private int CalculateScore(List<Card> cards, ZjhHandRank rank)
    {
        var sortedCards = cards.OrderByDescending(c => c.Rank).ToList();

        int score = 0;

        // 点数权重（从高到低：第1张*10000 + 第2张*100 + 第3张*1）
        for (int i = 0; i < 3; i++)
        {
            score += (int)sortedCards[i].Rank * (int)Math.Pow(10, 4 - i * 2);
        }

        // 加上最大花色权重（用于同点数决胜）
        int maxSuitRank = cards.Max(c => (int)c.Suit);
        score += maxSuitRank;

        return score;
    }

    /// <summary>
    /// 是否同花
    /// </summary>
    private bool IsFlush(List<Card> cards)
    {
        return cards.Select(c => c.Suit).Distinct().Count() == 1;
    }

    /// <summary>
    /// 是否顺子（A23最小，AKQ最大）
    /// </summary>
    private bool IsStraight(List<Card> sortedCards)
    {
        var ranks = sortedCards.Select(c => (int)c.Rank).OrderBy(r => r).ToList();

        // 特殊情况：A23（A=14, 2, 3）
        if (ranks[0] == 2 && ranks[1] == 3 && ranks[2] == 14)
            return true;

        // 正常顺子
        return ranks[2] - ranks[0] == 2 && ranks[2] - ranks[1] == 1;
    }

    /// <summary>
    /// 是否三条（豹子）
    /// </summary>
    private bool IsThreeOfAKind(List<Card> sortedCards)
    {
        return sortedCards[0].Rank == sortedCards[1].Rank
            && sortedCards[1].Rank == sortedCards[2].Rank;
    }

    /// <summary>
    /// 是否对子
    /// </summary>
    private bool IsPair(List<Card> sortedCards)
    {
        return sortedCards[0].Rank == sortedCards[1].Rank
            || sortedCards[1].Rank == sortedCards[2].Rank
            || sortedCards[0].Rank == sortedCards[2].Rank;
    }

    /// <summary>
    /// 获取牌型描述
    /// </summary>
    public string GetHandDescription(ZjhHandRank rank, List<Card> cards)
    {
        var rankNames = new Dictionary<ZjhHandRank, string>
        {
            [ZjhHandRank.ThreeOfAKind] = "豹子",
            [ZjhHandRank.StraightFlush] = "顺金",
            [ZjhHandRank.Flush] = "金花",
            [ZjhHandRank.Straight] = "顺子",
            [ZjhHandRank.Pair] = "对子",
            [ZjhHandRank.HighCard] = "高牌"
        };

        return rankNames.TryGetValue(rank, out var desc) ? desc : "未知";
    }
}
