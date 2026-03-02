using PokerGame.Domain.Enums;

namespace PokerGame.Domain.GameLogic;

/// <summary>
/// 牌组（一副52张牌）
/// </summary>
public class Deck
{
    private readonly List<Card> _cards;
    private int _currentIndex;

    /// <summary>
    /// 创建一副新牌（已洗牌）
    /// </summary>
    public Deck() : this(shuffle: true)
    {
    }

    /// <summary>
    /// 创建一副牌
    /// </summary>
    /// <param name="shuffle">是否洗牌</param>
    public Deck(bool shuffle)
    {
        _cards = new List<Card>();
        _currentIndex = 0;

        // 创建52张牌
        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
        {
            foreach (Rank rank in Enum.GetValues(typeof(Rank)))
            {
                _cards.Add(new Card(suit, rank));
            }
        }

        if (shuffle)
        {
            Shuffle();
        }
    }

    /// <summary>
    /// 洗牌
    /// </summary>
    public void Shuffle()
    {
        var random = new Random();
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
        _currentIndex = 0;
    }

    /// <summary>
    /// 发一张牌
    /// </summary>
    /// <returns>牌，如果牌组已空则返回null</returns>
    public Card? Deal()
    {
        if (_currentIndex >= _cards.Count)
        {
            return null;
        }
        return _cards[_currentIndex++];
    }

    /// <summary>
    /// 发多张牌
    /// </summary>
    /// <param name="count">数量</param>
    /// <returns>牌列表</returns>
    public List<Card> DealMultiple(int count)
    {
        var cards = new List<Card>();
        for (int i = 0; i < count; i++)
        {
            var card = Deal();
            if (card == null) break;
            cards.Add(card);
        }
        return cards;
    }

    /// <summary>
    /// 剩余牌数
    /// </summary>
    public int RemainingCount => _cards.Count - _currentIndex;

    /// <summary>
    /// 是否已空
    /// </summary>
    public bool IsEmpty => _currentIndex >= _cards.Count;
}
