using PokerGame.Domain.Enums;

namespace PokerGame.Domain.GameLogic;

/// <summary>
/// 扑克牌
/// </summary>
public class Card
{
    /// <summary>
    /// 花色
    /// </summary>
    public Suit Suit { get; }

    /// <summary>
    /// 牌面大小
    /// </summary>
    public Rank Rank { get; }

    public Card(Suit suit, Rank rank)
    {
        Suit = suit;
        Rank = rank;
    }

    /// <summary>
    /// 获取牌的显示名称
    /// </summary>
    public string DisplayName => $"{GetRankDisplay()}{GetSuitDisplay()}";

    private string GetRankDisplay() => Rank switch
    {
        Rank.Two => "2",
        Rank.Three => "3",
        Rank.Four => "4",
        Rank.Five => "5",
        Rank.Six => "6",
        Rank.Seven => "7",
        Rank.Eight => "8",
        Rank.Nine => "9",
        Rank.Ten => "10",
        Rank.Jack => "J",
        Rank.Queen => "Q",
        Rank.King => "K",
        Rank.Ace => "A",
        _ => Rank.ToString()
    };

    private string GetSuitDisplay() => Suit switch
    {
        Suit.Spades => "♠",
        Suit.Hearts => "♥",
        Suit.Clubs => "♣",
        Suit.Diamonds => "♦",
        _ => Suit.ToString()
    };

    /// <summary>
    /// 获取牌的数值（用于比较）
    /// </summary>
    public int Value => (int)Rank;

    public override string ToString() => DisplayName;

    public override bool Equals(object? obj)
    {
        if (obj is Card other)
        {
            return Suit == other.Suit && Rank == other.Rank;
        }
        return false;
    }

    public override int GetHashCode() => HashCode.Combine(Suit, Rank);
}
