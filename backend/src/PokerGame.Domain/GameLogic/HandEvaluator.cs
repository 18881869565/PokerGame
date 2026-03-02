using PokerGame.Domain.Enums;

namespace PokerGame.Domain.GameLogic;

/// <summary>
/// 德州扑克牌型评估器
/// </summary>
public static class HandEvaluator
{
    /// <summary>
    /// 评估最佳5张牌组合
    /// </summary>
    /// <param name="holeCards">手牌（2张）</param>
    /// <param name="communityCards">公共牌（最多5张）</param>
    /// <returns>最佳牌型评估结果</returns>
    public static HandEvaluation Evaluate(List<Card> holeCards, List<Card> communityCards)
    {
        var allCards = holeCards.Concat(communityCards).ToList();

        if (allCards.Count < 5)
        {
            return new HandEvaluation { Rank = HandRank.HighCard, Kickers = new List<int> { 0 } };
        }

        // 获取所有可能的5张牌组合
        var combinations = GetCombinations(allCards, 5);

        HandEvaluation? bestHand = null;

        foreach (var combo in combinations)
        {
            var evaluation = EvaluateFiveCards(combo);
            if (bestHand == null || evaluation.CompareTo(bestHand) > 0)
            {
                bestHand = evaluation;
            }
        }

        return bestHand!;
    }

    /// <summary>
    /// 评估5张牌的牌型
    /// </summary>
    private static HandEvaluation EvaluateFiveCards(List<Card> cards)
    {
        var sortedCards = cards.OrderByDescending(c => c.Value).ToList();
        var suits = sortedCards.Select(c => c.Suit).ToList();
        var values = sortedCards.Select(c => c.Value).ToList();

        bool isFlush = suits.Distinct().Count() == 1;
        bool isStraight = IsStraight(values);
        var groups = values.GroupBy(v => v).OrderByDescending(g => g.Count()).ThenByDescending(g => g.Key).ToList();

        // 皇家同花顺
        if (isFlush && IsRoyalStraight(values))
        {
            return new HandEvaluation { Rank = HandRank.RoyalFlush, Kickers = new List<int> { 14 } };
        }

        // 同花顺
        if (isFlush && isStraight)
        {
            return new HandEvaluation { Rank = HandRank.StraightFlush, Kickers = GetStraightKickers(values) };
        }

        // 四条
        if (groups[0].Count() == 4)
        {
            return new HandEvaluation
            {
                Rank = HandRank.FourOfAKind,
                Kickers = new List<int> { groups[0].Key, groups[1].Key }
            };
        }

        // 葫芦
        if (groups[0].Count() == 3 && groups[1].Count() == 2)
        {
            return new HandEvaluation
            {
                Rank = HandRank.FullHouse,
                Kickers = new List<int> { groups[0].Key, groups[1].Key }
            };
        }

        // 同花
        if (isFlush)
        {
            return new HandEvaluation { Rank = HandRank.Flush, Kickers = values };
        }

        // 顺子
        if (isStraight)
        {
            return new HandEvaluation { Rank = HandRank.Straight, Kickers = GetStraightKickers(values) };
        }

        // 三条
        if (groups[0].Count() == 3)
        {
            return new HandEvaluation
            {
                Rank = HandRank.ThreeOfAKind,
                Kickers = new List<int> { groups[0].Key, groups[1].Key, groups[2].Key }
            };
        }

        // 两对
        if (groups[0].Count() == 2 && groups[1].Count() == 2)
        {
            return new HandEvaluation
            {
                Rank = HandRank.TwoPair,
                Kickers = new List<int> { groups[0].Key, groups[1].Key, groups[2].Key }
            };
        }

        // 一对
        if (groups[0].Count() == 2)
        {
            return new HandEvaluation
            {
                Rank = HandRank.OnePair,
                Kickers = new List<int> { groups[0].Key, groups[1].Key, groups[2].Key, groups[3].Key }
            };
        }

        // 高牌
        return new HandEvaluation { Rank = HandRank.HighCard, Kickers = values };
    }

    /// <summary>
    /// 判断是否是顺子
    /// </summary>
    private static bool IsStraight(List<int> values)
    {
        var sorted = values.OrderBy(v => v).Distinct().ToList();

        // 普通顺子
        if (sorted.Count == 5 && sorted[4] - sorted[0] == 4)
        {
            return true;
        }

        // A2345 特殊顺子（A当1用）
        if (sorted.Contains(14) && sorted.Contains(2) && sorted.Contains(3) && sorted.Contains(4) && sorted.Contains(5))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 判断是否是皇家顺子（10-J-Q-K-A）
    /// </summary>
    private static bool IsRoyalStraight(List<int> values)
    {
        return values.Contains(10) && values.Contains(11) && values.Contains(12) &&
               values.Contains(13) && values.Contains(14);
    }

    /// <summary>
    /// 获取顺子的踢脚牌（考虑A当1用的情况）
    /// </summary>
    private static List<int> GetStraightKickers(List<int> values)
    {
        var sorted = values.OrderByDescending(v => v).ToList();

        // A2345 特殊情况，最大牌是5
        if (sorted[0] == 14 && sorted[4] == 2)
        {
            return new List<int> { 5 };
        }

        return new List<int> { sorted[0] };
    }

    /// <summary>
    /// 获取所有组合
    /// </summary>
    private static List<List<T>> GetCombinations<T>(List<T> items, int count)
    {
        var result = new List<List<T>>();

        if (count == 0)
        {
            result.Add(new List<T>());
            return result;
        }

        if (items.Count < count)
        {
            return result;
        }

        for (int i = 0; i <= items.Count - count; i++)
        {
            var current = items[i];
            var remaining = items.Skip(i + 1).ToList();
            var subCombos = GetCombinations(remaining, count - 1);

            foreach (var subCombo in subCombos)
            {
                var combo = new List<T> { current };
                combo.AddRange(subCombo);
                result.Add(combo);
            }
        }

        return result;
    }

    /// <summary>
    /// 比较多位玩家的手牌，返回获胜者索引列表（可能有平局）
    /// </summary>
    public static List<int> DetermineWinners(List<(int PlayerIndex, List<Card> HoleCards)> players, List<Card> communityCards)
    {
        var evaluations = players
            .Select(p => (p.PlayerIndex, Evaluation: Evaluate(p.HoleCards, communityCards)))
            .ToList();

        var bestEvaluation = evaluations.MaxBy(e => e.Evaluation).Evaluation;

        var winners = evaluations
            .Where(e => e.Evaluation.CompareTo(bestEvaluation) == 0)
            .Select(e => e.PlayerIndex)
            .ToList();

        return winners;
    }
}
