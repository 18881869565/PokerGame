using PokerGame.Domain.Enums;

namespace PokerGame.Domain.GameLogic;

/// <summary>
/// 德州扑克游戏引擎
/// </summary>
public class PokerEngine
{
    /// <summary>
    /// 开始新一局游戏
    /// </summary>
    public GameState StartNewGame(List<GamePlayer> players, int smallBlind, int bigBlind, int dealerIndex = -1)
    {
        if (players.Count < 2)
        {
            throw new InvalidOperationException("至少需要2名玩家才能开始游戏");
        }

        var state = new GameState
        {
            Phase = GamePhase.Starting,
            Deck = new Deck(),
            Players = players.OrderBy(p => p.SeatIndex).ToList(),
            SmallBlind = smallBlind,
            BigBlind = bigBlind,
            StartTime = DateTime.Now
        };

        // 重置所有玩家
        foreach (var player in state.Players)
        {
            player.ResetForNewRound();
        }

        // 设置庄家位
        if (dealerIndex < 0)
        {
            // 随机选择庄家
            var random = new Random();
            dealerIndex = random.Next(state.Players.Count);
        }
        state.DealerIndex = dealerIndex % state.Players.Count;
        state.Players[state.DealerIndex].IsDealer = true;

        // 设置小盲位（庄家左边）
        var smallBlindIndex = (state.DealerIndex + 1) % state.Players.Count;
        state.Players[smallBlindIndex].IsSmallBlind = true;

        // 设置大盲位（小盲左边）
        var bigBlindIndex = (state.DealerIndex + 2) % state.Players.Count;
        state.Players[bigBlindIndex].IsBigBlind = true;

        // 收取盲注
        CollectBlinds(state, smallBlindIndex, bigBlindIndex);

        // 进入PreFlop阶段
        state.Phase = GamePhase.PreFlop;

        // 发底牌
        DealHoleCards(state);

        // 设置第一个操作玩家（大盲左边的玩家，即UTG位置）
        state.CurrentPlayerIndex = (bigBlindIndex + 1) % state.Players.Count;
        state.FirstActorIndex = state.CurrentPlayerIndex;

        return state;
    }

    /// <summary>
    /// 收取盲注
    /// </summary>
    private void CollectBlinds(GameState state, int smallBlindIndex, int bigBlindIndex)
    {
        var smallBlindPlayer = state.Players[smallBlindIndex];
        var bigBlindPlayer = state.Players[bigBlindIndex];

        // 小盲注
        var smallBlindAmount = Math.Min(state.SmallBlind, smallBlindPlayer.Chips);
        smallBlindPlayer.Chips -= smallBlindAmount;
        smallBlindPlayer.CurrentBet = smallBlindAmount;
        smallBlindPlayer.TotalBet = smallBlindAmount;
        state.Pot += smallBlindAmount;

        // 大盲注
        var bigBlindAmount = Math.Min(state.BigBlind, bigBlindPlayer.Chips);
        bigBlindPlayer.Chips -= bigBlindAmount;
        bigBlindPlayer.CurrentBet = bigBlindAmount;
        bigBlindPlayer.TotalBet = bigBlindAmount;
        state.Pot += bigBlindAmount;

        // 设置当前最高下注为大盲
        state.CurrentHighestBet = state.BigBlind;
    }

    /// <summary>
    /// 发底牌
    /// </summary>
    private void DealHoleCards(GameState state)
    {
        foreach (var player in state.Players)
        {
            player.HoleCards = state.Deck.DealMultiple(2);
            player.Status = PlayerStatus.Waiting;
        }
    }

    /// <summary>
    /// 玩家过牌
    /// </summary>
    public GameStateResult Check(GameState state, long userId)
    {
        var player = state.GetPlayer(userId);
        if (player == null)
        {
            return GameStateResult.Fail("玩家不在游戏中");
        }

        if (state.CurrentPlayer?.UserId != userId)
        {
            return GameStateResult.Fail("还没轮到你操作");
        }

        if (player.CurrentBet < state.CurrentHighestBet)
        {
            return GameStateResult.Fail("当前无法过牌，需要跟注或弃牌");
        }

        player.LastAction = PlayerAction.Check;
        player.Status = PlayerStatus.Waiting;

        return AdvanceGame(state);
    }

    /// <summary>
    /// 玩家跟注
    /// </summary>
    public GameStateResult Call(GameState state, long userId)
    {
        var player = state.GetPlayer(userId);
        if (player == null)
        {
            return GameStateResult.Fail("玩家不在游戏中");
        }

        if (state.CurrentPlayer?.UserId != userId)
        {
            return GameStateResult.Fail("还没轮到你操作");
        }

        var callAmount = state.CurrentHighestBet - player.CurrentBet;

        if (callAmount <= 0)
        {
            // 不需要跟注，转为过牌
            return Check(state, userId);
        }

        // 检查筹码是否足够
        if (player.Chips < callAmount)
        {
            // 筹码不够，全押
            return AllIn(state, userId);
        }

        player.Chips -= callAmount;
        player.CurrentBet += callAmount;
        player.TotalBet += callAmount;
        state.Pot += callAmount;
        player.LastAction = PlayerAction.Call;
        player.Status = PlayerStatus.Waiting;

        return AdvanceGame(state);
    }

    /// <summary>
    /// 玩家加注
    /// </summary>
    public GameStateResult Raise(GameState state, long userId, long amount)
    {
        var player = state.GetPlayer(userId);
        if (player == null)
        {
            return GameStateResult.Fail("玩家不在游戏中");
        }

        if (state.CurrentPlayer?.UserId != userId)
        {
            return GameStateResult.Fail("还没轮到你操作");
        }

        // 加注总额 = 跟注金额 + 加注金额
        var callAmount = state.CurrentHighestBet - player.CurrentBet;
        var totalRaise = callAmount + amount;

        if (amount < state.BigBlind && player.Chips > totalRaise)
        {
            return GameStateResult.Fail($"加注金额不能小于大盲注({state.BigBlind})");
        }

        if (player.Chips < totalRaise)
        {
            return GameStateResult.Fail("筹码不足");
        }

        // 如果加注后剩余筹码为0，转为全押
        if (player.Chips == totalRaise)
        {
            return AllIn(state, userId);
        }

        player.Chips -= totalRaise;
        player.CurrentBet += totalRaise;
        player.TotalBet += totalRaise;
        state.Pot += totalRaise;
        state.CurrentHighestBet = player.CurrentBet;
        player.LastAction = PlayerAction.Raise;
        player.Status = PlayerStatus.Waiting;
        state.LastActionPlayerId = userId;

        return AdvanceGame(state);
    }

    /// <summary>
    /// 玩家弃牌
    /// </summary>
    public GameStateResult Fold(GameState state, long userId)
    {
        var player = state.GetPlayer(userId);
        if (player == null)
        {
            return GameStateResult.Fail("玩家不在游戏中");
        }

        if (state.CurrentPlayer?.UserId != userId)
        {
            return GameStateResult.Fail("还没轮到你操作");
        }

        player.Status = PlayerStatus.Folded;
        player.LastAction = PlayerAction.Fold;

        return AdvanceGame(state);
    }

    /// <summary>
    /// 玩家全押
    /// </summary>
    public GameStateResult AllIn(GameState state, long userId)
    {
        var player = state.GetPlayer(userId);
        if (player == null)
        {
            return GameStateResult.Fail("玩家不在游戏中");
        }

        if (state.CurrentPlayer?.UserId != userId)
        {
            return GameStateResult.Fail("还没轮到你操作");
        }

        var allInAmount = player.Chips;
        player.CurrentBet += allInAmount;
        player.TotalBet += allInAmount;
        state.Pot += allInAmount;
        player.Chips = 0;
        player.Status = PlayerStatus.AllIn;
        player.LastAction = PlayerAction.AllIn;

        // 如果全押金额超过当前最高下注，更新最高下注
        if (player.CurrentBet > state.CurrentHighestBet)
        {
            state.CurrentHighestBet = player.CurrentBet;
            state.LastActionPlayerId = userId;
        }

        return AdvanceGame(state);
    }

    /// <summary>
    /// 推进游戏
    /// </summary>
    private GameStateResult AdvanceGame(GameState state)
    {
        // 检查是否只剩一个玩家（其他人都弃牌）
        var activePlayers = state.GetActivePlayers();
        if (activePlayers.Count == 1)
        {
            // 只剩一个玩家，直接获胜
            state.Phase = GamePhase.Finished;
            return GameStateResult.Ok(state, GameEventType.PlayerWins, "只剩一名玩家，直接获胜");
        }

        // 检查本轮下注是否结束
        if (state.IsBettingRoundComplete())
        {
            // 进入下一阶段
            return AdvanceToNextPhase(state);
        }

        // 移动到下一个玩家
        state.CurrentPlayerIndex = state.GetNextPlayerIndex(state.CurrentPlayerIndex);
        if (state.CurrentPlayerIndex >= 0)
        {
            state.Players[state.CurrentPlayerIndex].Status = PlayerStatus.MyTurn;
        }

        return GameStateResult.Ok(state, GameEventType.TurnChanged, "轮到下一位玩家");
    }

    /// <summary>
    /// 进入下一阶段
    /// </summary>
    private GameStateResult AdvanceToNextPhase(GameState state)
    {
        // 重置所有玩家的当前下注
        foreach (var player in state.Players)
        {
            player.ResetBetForNewPhase();
            if (player.Status != PlayerStatus.AllIn && player.Status != PlayerStatus.Folded)
            {
                player.LastAction = null;
            }
        }
        state.CurrentHighestBet = 0;

        switch (state.Phase)
        {
            case GamePhase.PreFlop:
                // 发翻牌（3张公共牌）
                state.Phase = GamePhase.Flop;
                state.Deck.Deal(); // 烧一张牌
                state.CommunityCards.AddRange(state.Deck.DealMultiple(3));
                break;

            case GamePhase.Flop:
                // 发转牌（1张公共牌）
                state.Phase = GamePhase.Turn;
                state.Deck.Deal(); // 烧一张牌
                state.CommunityCards.AddRange(state.Deck.DealMultiple(1));
                break;

            case GamePhase.Turn:
                // 发河牌（1张公共牌）
                state.Phase = GamePhase.River;
                state.Deck.Deal(); // 烧一张牌
                state.CommunityCards.AddRange(state.Deck.DealMultiple(1));
                break;

            case GamePhase.River:
                // 进入摊牌
                state.Phase = GamePhase.Showdown;
                return DetermineWinner(state);

            default:
                return GameStateResult.Ok(state, GameEventType.PhaseChanged, $"进入阶段: {state.Phase}");
        }

        // 设置下一阶段的第一个操作玩家（从庄家左边第一个未弃牌的玩家开始）
        var actingPlayers = state.GetActingPlayers();
        if (actingPlayers.Count > 0)
        {
            // 从庄家左边开始找第一个可以操作的玩家
            state.CurrentPlayerIndex = state.GetNextPlayerIndex(state.DealerIndex);
            if (state.CurrentPlayerIndex >= 0)
            {
                state.Players[state.CurrentPlayerIndex].Status = PlayerStatus.MyTurn;
            }
            state.FirstActorIndex = state.CurrentPlayerIndex;
        }

        return GameStateResult.Ok(state, GameEventType.PhaseChanged, $"进入阶段: {state.Phase}");
    }

    /// <summary>
    /// 决定获胜者
    /// </summary>
    private GameStateResult DetermineWinner(GameState state)
    {
        var activePlayers = state.GetActivePlayers();

        if (activePlayers.Count == 1)
        {
            // 只有一个玩家，直接获胜
            state.Phase = GamePhase.Finished;
            return GameStateResult.Ok(state, GameEventType.GameEnded, $"玩家 {activePlayers[0].Nickname} 获胜");
        }

        // 评估所有玩家的牌型
        var playerHands = activePlayers.Select(p => (Player: p, Evaluation: HandEvaluator.Evaluate(p.HoleCards, state.CommunityCards))).ToList();

        // 找到最佳牌型
        var bestEvaluation = playerHands.MaxBy(h => h.Evaluation).Evaluation;

        // 找到所有持有最佳牌型的玩家（可能有平局）
        var winners = playerHands.Where(h => h.Evaluation.CompareTo(bestEvaluation) == 0).ToList();

        state.Phase = GamePhase.Finished;

        return GameStateResult.Ok(state, GameEventType.GameEnded, $"游戏结束，获胜者: {string.Join(", ", winners.Select(w => w.Player.Nickname))}");
    }

    /// <summary>
    /// 获取游戏结果
    /// </summary>
    public GameResult GetGameResult(GameState state)
    {
        var activePlayers = state.GetActivePlayers();

        if (activePlayers.Count == 1)
        {
            var winner = activePlayers[0];
            return new GameResult
            {
                Winners = new List<long> { winner.UserId },
                Pot = state.Pot,
                PlayerHands = state.Players.Select(p => new PlayerHandResult
                {
                    UserId = p.UserId,
                    HoleCards = p.HoleCards,
                    Evaluation = p.IsInGame ? HandEvaluator.Evaluate(p.HoleCards, state.CommunityCards) : null,
                    ChipsWon = p.UserId == winner.UserId ? state.Pot : 0
                }).ToList()
            };
        }

        // 多人摊牌
        var playerHands = activePlayers.Select(p => (
            Player: p,
            Evaluation: HandEvaluator.Evaluate(p.HoleCards, state.CommunityCards)
        )).ToList();

        var bestEvaluation = playerHands.MaxBy(h => h.Evaluation).Evaluation;
        var winners = playerHands.Where(h => h.Evaluation.CompareTo(bestEvaluation) == 0).ToList();

        // 分配底池（支持平局分割）
        var winAmountPerPlayer = state.Pot / winners.Count;

        return new GameResult
        {
            Winners = winners.Select(w => w.Player.UserId).ToList(),
            Pot = state.Pot,
            PlayerHands = state.Players.Select(p =>
            {
                var isWinner = winners.Any(w => w.Player.UserId == p.UserId);
                return new PlayerHandResult
                {
                    UserId = p.UserId,
                    HoleCards = p.HoleCards,
                    Evaluation = p.IsInGame ? HandEvaluator.Evaluate(p.HoleCards, state.CommunityCards) : null,
                    ChipsWon = isWinner ? winAmountPerPlayer : 0
                };
            }).ToList()
        };
    }
}

/// <summary>
/// 游戏状态操作结果
/// </summary>
public class GameStateResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public GameState? State { get; set; }
    public GameEventType EventType { get; set; }

    public static GameStateResult Ok(GameState state, GameEventType eventType, string message)
    {
        return new GameStateResult
        {
            IsSuccess = true,
            State = state,
            EventType = eventType,
            Message = message
        };
    }

    public static GameStateResult Fail(string message)
    {
        return new GameStateResult
        {
            IsSuccess = false,
            Message = message
        };
    }
}

/// <summary>
/// 游戏事件类型
/// </summary>
public enum GameEventType
{
    GameStarted,
    TurnChanged,
    PhaseChanged,
    PlayerAction,
    PlayerWins,
    GameEnded
}

/// <summary>
/// 游戏结果
/// </summary>
public class GameResult
{
    public List<long> Winners { get; set; } = new();
    public long Pot { get; set; }
    public List<PlayerHandResult> PlayerHands { get; set; } = new();
}

/// <summary>
/// 玩家手牌结果
/// </summary>
public class PlayerHandResult
{
    public long UserId { get; set; }
    public List<Card> HoleCards { get; set; } = new();
    public HandEvaluation? Evaluation { get; set; }
    public long ChipsWon { get; set; }
}
