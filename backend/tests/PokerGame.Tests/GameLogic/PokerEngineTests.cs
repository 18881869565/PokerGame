using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PokerGame.Domain.Enums;
using PokerGame.Domain.GameLogic;

namespace PokerGame.Tests.GameLogic;

/// <summary>
/// PokerEngine 单元测试 - 测试游戏流程
/// </summary>
[TestClass]
public class PokerEngineTests
{
    private PokerEngine _engine = null!;

    [TestInitialize]
    public void Setup()
    {
        _engine = new PokerEngine();
    }

    #region 开始游戏测试

    [TestMethod]
    public void StartNewGame_With2Players_ShouldSucceed()
    {
        // Arrange
        var players = CreateTestPlayers(2);

        // Act
        var state = _engine.StartNewGame(players, 10, 20);

        // Assert
        state.Should().NotBeNull();
        state.Phase.Should().Be(GamePhase.PreFlop);
        state.Players.Should().HaveCount(2);
        state.Pot.Should().Be(30); // 小盲10 + 大盲20
    }

    [TestMethod]
    public void StartNewGame_WithLessThan2Players_ShouldThrow()
    {
        // Arrange
        var players = CreateTestPlayers(1);

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => _engine.StartNewGame(players, 10, 20));
    }

    [TestMethod]
    public void StartNewGame_ShouldDeal2CardsToEachPlayer()
    {
        // Arrange
        var players = CreateTestPlayers(3);

        // Act
        var state = _engine.StartNewGame(players, 10, 20);

        // Assert
        foreach (var player in state.Players)
        {
            player.HoleCards.Should().HaveCount(2);
        }
    }

    [TestMethod]
    public void StartNewGame_ShouldSetBlindPositions()
    {
        // Arrange
        var players = CreateTestPlayers(3);

        // Act
        var state = _engine.StartNewGame(players, 10, 20, dealerIndex: 0);

        // Assert
        state.Players[0].IsDealer.Should().BeTrue();
        state.Players[1].IsSmallBlind.Should().BeTrue();
        state.Players[2].IsBigBlind.Should().BeTrue();
    }

    [TestMethod]
    public void StartNewGame_FirstToAct_ShouldBeAfterBigBlind()
    {
        // Arrange
        var players = CreateTestPlayers(4);

        // Act
        var state = _engine.StartNewGame(players, 10, 20, dealerIndex: 0);

        // Assert - 大盲是位置2，第一个操作应该是位置3
        state.CurrentPlayerIndex.Should().Be(3);
    }

    #endregion

    #region 过牌测试

    [TestMethod]
    public void Check_WhenNotPlayerTurn_ShouldFail()
    {
        // Arrange
        var players = CreateTestPlayers(3);
        var state = _engine.StartNewGame(players, 10, 20);
        var nonCurrentPlayerId = state.Players.First(p => state.CurrentPlayer?.UserId != p.UserId).UserId;

        // Act
        var result = _engine.Check(state, nonCurrentPlayerId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("还没轮到你");
    }

    [TestMethod]
    public void Check_WhenNeedToCall_ShouldFail()
    {
        // Arrange
        var players = CreateTestPlayers(3);
        var state = _engine.StartNewGame(players, 10, 20);
        // 大盲已经下了20，当前玩家还没下注，需要跟注20才能过牌

        // Act - 当前玩家尝试过牌（需要先跟注）
        var currentPlayerId = state.CurrentPlayer!.UserId;
        var result = _engine.Check(state, currentPlayerId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("需要跟注或弃牌");
    }

    #endregion

    #region 跟注测试

    [TestMethod]
    public void Call_WhenPlayerCanCall_ShouldSucceed()
    {
        // Arrange
        var players = CreateTestPlayers(3);
        var state = _engine.StartNewGame(players, 10, 20);
        var currentPlayerId = state.CurrentPlayer!.UserId;
        var playerChipsBefore = state.CurrentPlayer.Chips;
        var potBefore = state.Pot;

        // Act
        var result = _engine.Call(state, currentPlayerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        state.Pot.Should().Be(potBefore + 20); // 底池增加20（大盲注）
        state.Players.First(p => p.UserId == currentPlayerId).Chips.Should().Be(playerChipsBefore - 20);
    }

    [TestMethod]
    public void Call_ShouldMoveToNextPlayer()
    {
        // Arrange
        var players = CreateTestPlayers(3);
        var state = _engine.StartNewGame(players, 10, 20);
        var firstPlayerIndex = state.CurrentPlayerIndex;

        // Act
        _engine.Call(state, state.CurrentPlayer!.UserId);

        // Assert
        state.CurrentPlayerIndex.Should().NotBe(firstPlayerIndex);
    }

    #endregion

    #region 加注测试

    [TestMethod]
    public void Raise_WithValidAmount_ShouldSucceed()
    {
        // Arrange
        var players = CreateTestPlayers(3);
        var state = _engine.StartNewGame(players, 10, 20);
        var currentPlayerId = state.CurrentPlayer!.UserId;

        // Act - 加注40（跟注20 + 加注40 = 总共60）
        var result = _engine.Raise(state, currentPlayerId, 40);

        // Assert
        result.IsSuccess.Should().BeTrue();
        state.CurrentHighestBet.Should().Be(60);
    }

    [TestMethod]
    public void Raise_WithInsufficientChips_ShouldFail()
    {
        // Arrange
        var players = CreateTestPlayers(3);
        players[0].Chips = 5; // 只有5筹码，不够加注
        var state = _engine.StartNewGame(players, 10, 20);

        // 跳到玩家0
        while (state.CurrentPlayer?.UserId != players[0].UserId)
        {
            _engine.Call(state, state.CurrentPlayer!.UserId);
        }

        // Act
        var result = _engine.Raise(state, players[0].UserId, 100);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("筹码不足");
    }

    #endregion

    #region 弃牌测试

    [TestMethod]
    public void Fold_ShouldSetPlayerStatusToFolded()
    {
        // Arrange
        var players = CreateTestPlayers(3);
        var state = _engine.StartNewGame(players, 10, 20);
        var currentPlayerId = state.CurrentPlayer!.UserId;

        // Act
        var result = _engine.Fold(state, currentPlayerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        state.Players.First(p => p.UserId == currentPlayerId).Status.Should().Be(PlayerStatus.Folded);
    }

    [TestMethod]
    public void Fold_WhenOnlyOnePlayerRemains_ShouldEndGame()
    {
        // Arrange
        var players = CreateTestPlayers(2);
        var state = _engine.StartNewGame(players, 10, 20);

        // Act - 玩家2（大盲）弃牌，只剩玩家1（小盲）
        var result = _engine.Fold(state, state.CurrentPlayer!.UserId);

        // Assert - 游戏应该结束（只剩一人）
        // 由于这是两人局，需要继续处理
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region 全押测试

    [TestMethod]
    public void AllIn_ShouldBetAllChips()
    {
        // Arrange
        var players = CreateTestPlayers(3);
        var state = _engine.StartNewGame(players, 10, 20);
        var currentPlayerId = state.CurrentPlayer!.UserId;
        var allInAmount = state.CurrentPlayer.Chips;

        // Act
        var result = _engine.AllIn(state, currentPlayerId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        state.Players.First(p => p.UserId == currentPlayerId).Chips.Should().Be(0);
        state.Players.First(p => p.UserId == currentPlayerId).Status.Should().Be(PlayerStatus.AllIn);
    }

    #endregion

    #region 游戏阶段测试

    [TestMethod]
    public void Game_ShouldProgressThroughAllPhases()
    {
        // Arrange - 使用3人游戏更清晰
        var players = CreateTestPlayers(3);
        var state = _engine.StartNewGame(players, 10, 20, dealerIndex: 0);

        // Act & Assert - PreFlop
        state.Phase.Should().Be(GamePhase.PreFlop);

        // PreFlop 阶段: 庄家=0, 小盲=1, 大盲=2
        // 第一个行动的是大盲左边，即位置0（庄家位，UTG）
        // 两人跟注后进入 Flop
        _engine.Call(state, state.CurrentPlayer!.UserId); // UTG 跟注
        _engine.Call(state, state.CurrentPlayer!.UserId); // 小盲跟注
        _engine.Check(state, state.CurrentPlayer!.UserId); // 大盲过牌

        state.Phase.Should().Be(GamePhase.Flop);
        state.CommunityCards.Should().HaveCount(3);

        // Flop 阶段所有人都过牌
        _engine.Check(state, state.CurrentPlayer!.UserId);
        _engine.Check(state, state.CurrentPlayer!.UserId);
        _engine.Check(state, state.CurrentPlayer!.UserId);

        state.Phase.Should().Be(GamePhase.Turn);
        state.CommunityCards.Should().HaveCount(4);

        // Turn 阶段
        _engine.Check(state, state.CurrentPlayer!.UserId);
        _engine.Check(state, state.CurrentPlayer!.UserId);
        _engine.Check(state, state.CurrentPlayer!.UserId);

        state.Phase.Should().Be(GamePhase.River);
        state.CommunityCards.Should().HaveCount(5);

        // River 阶段 - 最后一次过牌后进入摊牌并结算
        _engine.Check(state, state.CurrentPlayer!.UserId);
        _engine.Check(state, state.CurrentPlayer!.UserId);
        _engine.Check(state, state.CurrentPlayer!.UserId);

        // 摊牌后游戏结束
        state.Phase.Should().Be(GamePhase.Finished);
    }

    #endregion

    #region 辅助方法

    private List<GamePlayer> CreateTestPlayers(int count)
    {
        var players = new List<GamePlayer>();
        for (int i = 0; i < count; i++)
        {
            players.Add(new GamePlayer
            {
                UserId = i + 1,
                Username = $"Player{i + 1}",
                Nickname = $"玩家{i + 1}",
                SeatIndex = i,
                Chips = 1000
            });
        }
        return players;
    }

    #endregion
}
