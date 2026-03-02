using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PokerGame.Domain.Enums;
using PokerGame.Domain.GameLogic;

namespace PokerGame.Tests.GameLogic;

/// <summary>
/// HandEvaluator 单元测试 - 测试各种牌型评估
/// </summary>
[TestClass]
public class HandEvaluatorTests
{
    #region 高牌测试

    [TestMethod]
    public void Evaluate_HighCard_ShouldReturnHighCard()
    {
        // Arrange - 高牌 A-K-9-5-2 不同花
        var holeCards = new List<Card>
        {
            new(Suit.Hearts, Rank.Ace),
            new(Suit.Diamonds, Rank.Nine)
        };
        var communityCards = new List<Card>
        {
            new(Suit.Clubs, Rank.King),
            new(Suit.Spades, Rank.Five),
            new(Suit.Hearts, Rank.Two),
            new(Suit.Diamonds, Rank.Seven),
            new(Suit.Clubs, Rank.Three)
        };

        // Act
        var result = HandEvaluator.Evaluate(holeCards, communityCards);

        // Assert
        result.Rank.Should().Be(HandRank.HighCard);
    }

    #endregion

    #region 一对测试

    [TestMethod]
    public void Evaluate_OnePair_ShouldReturnOnePair()
    {
        // Arrange - 一对 A
        var holeCards = new List<Card>
        {
            new(Suit.Hearts, Rank.Ace),
            new(Suit.Diamonds, Rank.Ace)
        };
        var communityCards = new List<Card>
        {
            new(Suit.Clubs, Rank.King),
            new(Suit.Spades, Rank.Five),
            new(Suit.Hearts, Rank.Two),
            new(Suit.Diamonds, Rank.Seven),
            new(Suit.Clubs, Rank.Three)
        };

        // Act
        var result = HandEvaluator.Evaluate(holeCards, communityCards);

        // Assert
        result.Rank.Should().Be(HandRank.OnePair);
    }

    #endregion

    #region 两对测试

    [TestMethod]
    public void Evaluate_TwoPair_ShouldReturnTwoPair()
    {
        // Arrange - 两对 A-K
        var holeCards = new List<Card>
        {
            new(Suit.Hearts, Rank.Ace),
            new(Suit.Diamonds, Rank.King)
        };
        var communityCards = new List<Card>
        {
            new(Suit.Clubs, Rank.Ace),
            new(Suit.Spades, Rank.King),
            new(Suit.Hearts, Rank.Two),
            new(Suit.Diamonds, Rank.Seven),
            new(Suit.Clubs, Rank.Three)
        };

        // Act
        var result = HandEvaluator.Evaluate(holeCards, communityCards);

        // Assert
        result.Rank.Should().Be(HandRank.TwoPair);
    }

    #endregion

    #region 三条测试

    [TestMethod]
    public void Evaluate_ThreeOfAKind_ShouldReturnThreeOfAKind()
    {
        // Arrange - 三条 A
        var holeCards = new List<Card>
        {
            new(Suit.Hearts, Rank.Ace),
            new(Suit.Diamonds, Rank.Ace)
        };
        var communityCards = new List<Card>
        {
            new(Suit.Clubs, Rank.Ace),
            new(Suit.Spades, Rank.Five),
            new(Suit.Hearts, Rank.Two),
            new(Suit.Diamonds, Rank.Seven),
            new(Suit.Clubs, Rank.Three)
        };

        // Act
        var result = HandEvaluator.Evaluate(holeCards, communityCards);

        // Assert
        result.Rank.Should().Be(HandRank.ThreeOfAKind);
    }

    #endregion

    #region 顺子测试

    [TestMethod]
    public void Evaluate_Straight_ShouldReturnStraight()
    {
        // Arrange - 顺子 5-6-7-8-9
        var holeCards = new List<Card>
        {
            new(Suit.Hearts, Rank.Five),
            new(Suit.Diamonds, Rank.Six)
        };
        var communityCards = new List<Card>
        {
            new(Suit.Clubs, Rank.Seven),
            new(Suit.Spades, Rank.Eight),
            new(Suit.Hearts, Rank.Nine),
            new(Suit.Diamonds, Rank.King),
            new(Suit.Clubs, Rank.Three)
        };

        // Act
        var result = HandEvaluator.Evaluate(holeCards, communityCards);

        // Assert
        result.Rank.Should().Be(HandRank.Straight);
    }

    [TestMethod]
    public void Evaluate_WheelStraight_ShouldReturnStraight()
    {
        // Arrange - 轮子顺子 A-2-3-4-5
        var holeCards = new List<Card>
        {
            new(Suit.Hearts, Rank.Ace),
            new(Suit.Diamonds, Rank.Two)
        };
        var communityCards = new List<Card>
        {
            new(Suit.Clubs, Rank.Three),
            new(Suit.Spades, Rank.Four),
            new(Suit.Hearts, Rank.Five),
            new(Suit.Diamonds, Rank.King),
            new(Suit.Clubs, Rank.Nine)
        };

        // Act
        var result = HandEvaluator.Evaluate(holeCards, communityCards);

        // Assert
        result.Rank.Should().Be(HandRank.Straight);
    }

    #endregion

    #region 同花测试

    [TestMethod]
    public void Evaluate_Flush_ShouldReturnFlush()
    {
        // Arrange - 同花（5张红桃）
        var holeCards = new List<Card>
        {
            new(Suit.Hearts, Rank.Ace),
            new(Suit.Hearts, Rank.King)
        };
        var communityCards = new List<Card>
        {
            new(Suit.Hearts, Rank.Queen),
            new(Suit.Hearts, Rank.Jack),
            new(Suit.Hearts, Rank.Nine),
            new(Suit.Diamonds, Rank.Three),
            new(Suit.Clubs, Rank.Two)
        };

        // Act
        var result = HandEvaluator.Evaluate(holeCards, communityCards);

        // Assert
        result.Rank.Should().Be(HandRank.Flush);
    }

    #endregion

    #region 葫芦测试

    [TestMethod]
    public void Evaluate_FullHouse_ShouldReturnFullHouse()
    {
        // Arrange - 葫芦 A-A-A-K-K
        var holeCards = new List<Card>
        {
            new(Suit.Hearts, Rank.Ace),
            new(Suit.Diamonds, Rank.King)
        };
        var communityCards = new List<Card>
        {
            new(Suit.Clubs, Rank.Ace),
            new(Suit.Spades, Rank.Ace),
            new(Suit.Hearts, Rank.King),
            new(Suit.Diamonds, Rank.Three),
            new(Suit.Clubs, Rank.Two)
        };

        // Act
        var result = HandEvaluator.Evaluate(holeCards, communityCards);

        // Assert
        result.Rank.Should().Be(HandRank.FullHouse);
    }

    #endregion

    #region 四条测试

    [TestMethod]
    public void Evaluate_FourOfAKind_ShouldReturnFourOfAKind()
    {
        // Arrange - 四条 A
        var holeCards = new List<Card>
        {
            new(Suit.Hearts, Rank.Ace),
            new(Suit.Diamonds, Rank.Ace)
        };
        var communityCards = new List<Card>
        {
            new(Suit.Clubs, Rank.Ace),
            new(Suit.Spades, Rank.Ace),
            new(Suit.Hearts, Rank.King),
            new(Suit.Diamonds, Rank.Three),
            new(Suit.Clubs, Rank.Two)
        };

        // Act
        var result = HandEvaluator.Evaluate(holeCards, communityCards);

        // Assert
        result.Rank.Should().Be(HandRank.FourOfAKind);
    }

    #endregion

    #region 同花顺测试

    [TestMethod]
    public void Evaluate_StraightFlush_ShouldReturnStraightFlush()
    {
        // Arrange - 同花顺 5-6-7-8-9 黑桃
        var holeCards = new List<Card>
        {
            new(Suit.Spades, Rank.Five),
            new(Suit.Spades, Rank.Six)
        };
        var communityCards = new List<Card>
        {
            new(Suit.Spades, Rank.Seven),
            new(Suit.Spades, Rank.Eight),
            new(Suit.Spades, Rank.Nine),
            new(Suit.Hearts, Rank.King),
            new(Suit.Diamonds, Rank.Three)
        };

        // Act
        var result = HandEvaluator.Evaluate(holeCards, communityCards);

        // Assert
        result.Rank.Should().Be(HandRank.StraightFlush);
    }

    #endregion

    #region 皇家同花顺测试

    [TestMethod]
    public void Evaluate_RoyalFlush_ShouldReturnRoyalFlush()
    {
        // Arrange - 皇家同花顺 10-J-Q-K-A 黑桃
        var holeCards = new List<Card>
        {
            new(Suit.Spades, Rank.Ace),
            new(Suit.Spades, Rank.King)
        };
        var communityCards = new List<Card>
        {
            new(Suit.Spades, Rank.Queen),
            new(Suit.Spades, Rank.Jack),
            new(Suit.Spades, Rank.Ten),
            new(Suit.Hearts, Rank.Nine),
            new(Suit.Diamonds, Rank.Three)
        };

        // Act
        var result = HandEvaluator.Evaluate(holeCards, communityCards);

        // Assert
        result.Rank.Should().Be(HandRank.RoyalFlush);
    }

    #endregion

    #region 比较测试

    [TestMethod]
    public void HandEvaluation_Compare_ShouldReturnCorrectOrder()
    {
        // Arrange
        var highCard = new HandEvaluation { Rank = HandRank.HighCard, Kickers = new List<int> { 14, 13, 9 } };
        var onePair = new HandEvaluation { Rank = HandRank.OnePair, Kickers = new List<int> { 14, 13 } };
        var twoPair = new HandEvaluation { Rank = HandRank.TwoPair, Kickers = new List<int> { 14, 13 } };

        // Act & Assert
        onePair.CompareTo(highCard).Should().BePositive("一对应该大于高牌");
        twoPair.CompareTo(onePair).Should().BePositive("两对应该大于一对");
    }

    [TestMethod]
    public void HandEvaluation_Compare_SameRankShouldCompareKickers()
    {
        // Arrange
        var pair1 = new HandEvaluation { Rank = HandRank.OnePair, Kickers = new List<int> { 14, 13, 9 } };
        var pair2 = new HandEvaluation { Rank = HandRank.OnePair, Kickers = new List<int> { 14, 13, 8 } };

        // Act & Assert
        pair1.CompareTo(pair2).Should().BePositive("踢脚牌更高的应该更大");
    }

    #endregion

    #region 确定获胜者测试

    [TestMethod]
    public void DetermineWinners_OneWinner_ShouldReturnCorrectWinner()
    {
        // Arrange - 玩家1有对A，玩家2有高牌K-Q
        var players = new List<(int, List<Card>)>
        {
            (0, new List<Card> { new(Suit.Hearts, Rank.Ace), new(Suit.Diamonds, Rank.Ace) }),
            (1, new List<Card> { new(Suit.Clubs, Rank.King), new(Suit.Spades, Rank.Queen) })
        };
        // 公共牌不能形成顺子
        var communityCards = new List<Card>
        {
            new(Suit.Hearts, Rank.Two),
            new(Suit.Clubs, Rank.Seven),
            new(Suit.Diamonds, Rank.Nine),
            new(Suit.Spades, Rank.Jack),
            new(Suit.Hearts, Rank.King)
        };

        // Act
        var winners = HandEvaluator.DetermineWinners(players, communityCards);

        // Assert - 玩家0有一对A，玩家1有一对K，玩家0获胜
        winners.Should().ContainSingle().Which.Should().Be(0);
    }

    [TestMethod]
    public void DetermineWinners_Tie_ShouldReturnBothWinners()
    {
        // Arrange - 两个玩家都有对A，平局
        var players = new List<(int, List<Card>)>
        {
            (0, new List<Card> { new(Suit.Hearts, Rank.Ace), new(Suit.Diamonds, Rank.Two) }),
            (1, new List<Card> { new(Suit.Clubs, Rank.Ace), new(Suit.Spades, Rank.Two) })
        };
        var communityCards = new List<Card>
        {
            new(Suit.Hearts, Rank.King),
            new(Suit.Clubs, Rank.Queen),
            new(Suit.Diamonds, Rank.Jack),
            new(Suit.Spades, Rank.Nine),
            new(Suit.Hearts, Rank.Eight)
        };

        // Act
        var winners = HandEvaluator.DetermineWinners(players, communityCards);

        // Assert
        winners.Should().HaveCount(2);
        winners.Should().Contain(0);
        winners.Should().Contain(1);
    }

    #endregion
}
