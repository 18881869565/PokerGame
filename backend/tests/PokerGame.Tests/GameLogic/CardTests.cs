using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PokerGame.Domain.Enums;
using PokerGame.Domain.GameLogic;

namespace PokerGame.Tests.GameLogic;

/// <summary>
/// Card 类单元测试
/// </summary>
[TestClass]
public class CardTests
{
    [TestMethod]
    public void Card_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var card = new Card(Suit.Hearts, Rank.Ace);

        // Assert
        card.Suit.Should().Be(Suit.Hearts);
        card.Rank.Should().Be(Rank.Ace);
    }

    [TestMethod]
    public void Card_DisplayName_ShouldReturnCorrectFormat()
    {
        // Arrange & Act
        var card = new Card(Suit.Hearts, Rank.Ace);

        // Assert
        card.DisplayName.Should().Be("A♥");
    }

    [TestMethod]
    public void Card_DisplayName_ShouldHandleAllSuits()
    {
        // Arrange & Act & Assert
        new Card(Suit.Spades, Rank.King).DisplayName.Should().Be("K♠");
        new Card(Suit.Hearts, Rank.Queen).DisplayName.Should().Be("Q♥");
        new Card(Suit.Clubs, Rank.Jack).DisplayName.Should().Be("J♣");
        new Card(Suit.Diamonds, Rank.Ten).DisplayName.Should().Be("10♦");
    }

    [TestMethod]
    public void Card_Value_ShouldReturnCorrectValue()
    {
        // Arrange & Act & Assert
        new Card(Suit.Spades, Rank.Two).Value.Should().Be(2);
        new Card(Suit.Spades, Rank.Ace).Value.Should().Be(14);
        new Card(Suit.Spades, Rank.King).Value.Should().Be(13);
    }

    [TestMethod]
    public void Card_Equals_ShouldReturnTrueForSameCard()
    {
        // Arrange
        var card1 = new Card(Suit.Hearts, Rank.Ace);
        var card2 = new Card(Suit.Hearts, Rank.Ace);

        // Act & Assert
        card1.Equals(card2).Should().BeTrue();
    }

    [TestMethod]
    public void Card_Equals_ShouldReturnFalseForDifferentCard()
    {
        // Arrange
        var card1 = new Card(Suit.Hearts, Rank.Ace);
        var card2 = new Card(Suit.Spades, Rank.Ace);

        // Act & Assert
        card1.Equals(card2).Should().BeFalse();
    }
}
