using PokerGame.Domain.Enums;

namespace PokerGame.Domain.GameLogic;

/// <summary>
/// 扎金花游戏引擎
/// </summary>
public class ZjhGameEngine
{
    private readonly ZjhHandEvaluator _evaluator = new();
    private readonly Random _random = new();

    /// <summary>
    /// 游戏状态
    /// </summary>
    public ZjhGameState State { get; private set; }

    public ZjhGameEngine(ZjhGameState state)
    {
        State = state;
    }

    /// <summary>
    /// 创建新的游戏引擎
    /// </summary>
    public static ZjhGameEngine Create(long roomId, long anteAmount = 1, int maxRounds = 10)
    {
        var state = new ZjhGameState
        {
            RoomId = roomId,
            AnteAmount = anteAmount,
            BetAmount = anteAmount,
            MaxRounds = maxRounds,
            Deck = new Deck()
        };

        return new ZjhGameEngine(state);
    }

    /// <summary>
    /// 添加玩家
    /// </summary>
    public void AddPlayer(long userId, string username, string nickname, int seatIndex, long chips)
    {
        if (State.Phase != ZjhGamePhase.Waiting)
            throw new InvalidOperationException("游戏已开始，无法添加玩家");

        if (State.Players.Any(p => p.UserId == userId))
            throw new InvalidOperationException("玩家已在游戏中");

        State.Players.Add(new ZjhPlayer
        {
            UserId = userId,
            Username = username,
            Nickname = nickname,
            SeatIndex = seatIndex,
            Chips = chips
        });
    }

    /// <summary>
    /// 开始游戏
    /// </summary>
    public void StartGame()
    {
        if (State.Players.Count < 2)
            throw new InvalidOperationException("至少需要2名玩家才能开始游戏");

        if (State.Phase != ZjhGamePhase.Waiting)
            throw new InvalidOperationException("游戏已经开始");

        // 重置状态
        State.Deck = new Deck();
        State.Pot = 0;
        State.RoundCount = 0;
        State.WinnerId = null;
        State.WinnerHand = null;

        foreach (var player in State.Players)
        {
            player.ResetForNewRound();
        }

        // 收取底注
        foreach (var player in State.Players)
        {
            if (player.DeductChips(State.AnteAmount))
            {
                State.Pot += State.AnteAmount;
            }
        }

        // 发牌
        State.Phase = ZjhGamePhase.Dealing;
        foreach (var player in State.Players)
        {
            player.Hand = State.Deck.DealMultiple(3);
        }

        // 设置第一个玩家操作
        State.CurrentPlayerIndex = 0;
        State.Phase = ZjhGamePhase.Betting;
    }

    /// <summary>
    /// 玩家看牌（随时可以看，不要求轮到自己）
    /// </summary>
    public void LookCards(long userId)
    {
        var player = State.GetPlayer(userId)
            ?? throw new InvalidOperationException("玩家不在游戏中");

        if (player.HasLooked)
            throw new InvalidOperationException("已经看过牌了");

        if (player.IsFolded || player.IsOut || player.IsCompareLose)
            throw new InvalidOperationException("玩家已无法操作");

        player.HasLooked = true;
        player.LastAction = ZjhAction.Look;
        Console.WriteLine($"[ZjhGameEngine] Player {userId} looked at cards");
    }

    /// <summary>
    /// 玩家下注
    /// </summary>
    public void Bet(long userId)
    {
        ValidatePlayerTurn(userId);

        var player = State.CurrentPlayer!;
        var betAmount = State.BetAmount * player.BetMultiplier;

        if (player.Chips < betAmount)
            throw new InvalidOperationException("筹码不足");

        player.DeductChips(betAmount);
        State.Pot += betAmount;
        player.LastAction = player.HasLooked ? ZjhAction.BetLook : ZjhAction.BetBlind;

        MoveToNextPlayer();
        State.RoundCount++;
    }

    /// <summary
    /// 玩家加注（提高基础下注额）
    /// </summary>
    public void Raise(long userId, long newBetAmount)
    {
        ValidatePlayerTurn(userId);

        var player = State.CurrentPlayer!;
        var betAmount = newBetAmount * player.BetMultiplier;

        if (player.Chips < betAmount)
            throw new InvalidOperationException("筹码不足");

        if (newBetAmount <= State.BetAmount)
            throw new InvalidOperationException("加注金额必须大于当前下注额");

        // 计算需要支付的金额（新下注额 - 当前下注额）
        var actualBet = newBetAmount * player.BetMultiplier;
        player.DeductChips(actualBet);
        State.Pot += actualBet;
        State.BetAmount = newBetAmount;
        player.LastAction = player.HasLooked ? ZjhAction.BetLook : ZjhAction.BetBlind;

        MoveToNextPlayer();
        State.RoundCount++;
    }

    /// <summary>
    /// 玩家比牌
    /// </summary>
    public void Compare(long userId, long targetUserId)
    {
        ValidatePlayerTurn(userId);

        var player = State.CurrentPlayer!;
        var targetPlayer = State.GetPlayer(targetUserId)
            ?? throw new InvalidOperationException("目标玩家不在游戏中");

        if (!targetPlayer.IsInGame)
            throw new InvalidOperationException("目标玩家已不在游戏中");

        if (player.UserId == targetUserId)
            throw new InvalidOperationException("不能和自己比牌");

        // 比牌需要支付当前下注额的双倍
        var compareCost = State.BetAmount * player.BetMultiplier * 2;
        if (player.Chips < compareCost)
            throw new InvalidOperationException("筹码不足以比牌");

        player.DeductChips(compareCost);
        State.Pot += compareCost;
        player.LastAction = ZjhAction.Compare;

        // 执行比牌
        var result = _evaluator.Compare(player.Hand, targetPlayer.Hand);
        Console.WriteLine($"[ZjhGameEngine] Compare: {player.Nickname} vs {targetPlayer.Nickname}, result={result}");

        if (result < 0)
        {
            // 当前玩家输了，标记为比牌输掉（不是主动弃牌）
            player.IsCompareLose = true;
            player.LastAction = ZjhAction.Fold;
            Console.WriteLine($"[ZjhGameEngine] {player.Nickname} 比牌输了");
        }
        else
        {
            // 目标玩家输了（相等时发起者赢），标记为比牌输掉
            targetPlayer.IsCompareLose = true;
            targetPlayer.LastAction = ZjhAction.Fold;
            Console.WriteLine($"[ZjhGameEngine] {targetPlayer.Nickname} 比牌输了");
        }

        // 检查是否只剩一人
        if (CheckGameEnd())
        {
            return;
        }

        MoveToNextPlayer();
    }

    /// <summary>
    /// 玩家弃牌
    /// </summary>
    public void Fold(long userId)
    {
        ValidatePlayerTurn(userId);

        var player = State.CurrentPlayer!;
        player.IsFolded = true;
        player.LastAction = ZjhAction.Fold;

        // 检查是否只剩一人
        if (CheckGameEnd())
        {
            return;
        }

        MoveToNextPlayer();
    }

    /// <summary>
    /// 玩家全押
    /// </summary>
    public void AllIn(long userId)
    {
        ValidatePlayerTurn(userId);

        var player = State.CurrentPlayer!;

        if (player.Chips <= 0)
            throw new InvalidOperationException("没有筹码可全押");

        var allInAmount = player.Chips;
        State.Pot += allInAmount;
        player.TotalBet += allInAmount;
        player.Chips = 0;
        player.LastAction = ZjhAction.AllIn;

        MoveToNextPlayer();
        State.RoundCount++;
    }

    /// <summary>
    /// 验证是否轮到该玩家操作
    /// </summary>
    private void ValidatePlayerTurn(long userId)
    {
        if (State.Phase != ZjhGamePhase.Betting)
            throw new InvalidOperationException("当前不是下注阶段");

        if (State.CurrentPlayer?.UserId != userId)
            throw new InvalidOperationException("不是你的回合");

        var player = State.CurrentPlayer;
        if (player.IsFolded || player.IsOut || player.IsCompareLose)
            throw new InvalidOperationException("玩家已无法操作");
    }

    /// <summary>
    /// 移动到下一个玩家
    /// </summary>
    private void MoveToNextPlayer()
    {
        // 检查是否达到最大轮数
        if (State.RoundCount >= State.MaxRounds)
        {
            ForceShowdown();
            return;
        }

        var nextIndex = State.GetNextPlayerIndex(State.CurrentPlayerIndex);
        if (nextIndex == -1)
        {
            // 只剩一人，游戏结束
            EndGame();
            return;
        }

        State.CurrentPlayerIndex = nextIndex;
    }

    /// <summary>
    /// 检查游戏是否结束
    /// </summary>
    private bool CheckGameEnd()
    {
        var activePlayers = State.GetActivePlayers();

        if (activePlayers.Count == 1)
        {
            // 只剩一人，直接获胜
            var winner = activePlayers[0];
            State.WinnerId = winner.UserId;
            State.WinnerHand = winner.Hand;
            EndGame();
            return true;
        }

        if (activePlayers.Count == 0)
        {
            // 所有人都出局，游戏结束（异常情况）
            EndGame();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 强制摊牌（达到最大轮数）
    /// </summary>
    private void ForceShowdown()
    {
        State.Phase = ZjhGamePhase.Comparing;

        var activePlayers = State.GetActivePlayers();
        if (activePlayers.Count == 0)
        {
            EndGame();
            return;
        }

        // 找出最大的牌
        ZjhPlayer? winner = null;
        int bestScore = -1;

        foreach (var player in activePlayers)
        {
            var eval = _evaluator.Evaluate(player.Hand);
            var score = (int)eval.Rank * 1000000 + eval.Score;

            if (score > bestScore)
            {
                bestScore = score;
                winner = player;
            }
        }

        if (winner != null)
        {
            State.WinnerId = winner.UserId;
            State.WinnerHand = winner.Hand;
        }

        EndGame();
    }

    /// <summary>
    /// 结束游戏
    /// </summary>
    private void EndGame()
    {
        State.Phase = ZjhGamePhase.Finished;

        // 将底池给赢家
        if (State.WinnerId.HasValue)
        {
            var winner = State.GetPlayer(State.WinnerId.Value);
            if (winner != null)
            {
                winner.Chips += State.Pot;
            }
        }
    }

    /// <summary>
    /// 获取玩家可用的操作列表
    /// </summary>
    public List<ZjhAction> GetAvailableActions(long userId)
    {
        var actions = new List<ZjhAction>();
        var player = State.GetPlayer(userId);

        if (player == null || !player.IsInGame)
        {
            Console.WriteLine($"[ZjhGameEngine] GetAvailableActions: player is null or not in game, userId={userId}");
            return actions;
        }

        if (State.Phase != ZjhGamePhase.Betting)
        {
            Console.WriteLine($"[ZjhGameEngine] GetAvailableActions: not in betting phase, phase={State.Phase}");
            return actions;
        }

        if (State.CurrentPlayer?.UserId != userId)
        {
            Console.WriteLine($"[ZjhGameEngine] GetAvailableActions: not player's turn, current={State.CurrentPlayer?.UserId}, request={userId}");
            return actions;
        }

        // 看牌（如果还没看）
        if (!player.HasLooked)
            actions.Add(ZjhAction.Look);

        // 下注
        if (player.Chips >= State.BetAmount * player.BetMultiplier)
            actions.Add(ZjhAction.BetBlind);

        // 比牌（至少需要2人才能比牌，且首轮不能比牌）
        var activePlayers = State.GetActivePlayers();
        var compareCost = State.BetAmount * player.BetMultiplier * 2;
        Console.WriteLine($"[ZjhGameEngine] GetAvailableActions: roundCount={State.RoundCount}, activePlayers={activePlayers.Count}, chips={player.Chips}, compareCost={compareCost}");

        // 首轮不允许比牌：需要每个玩家至少完成一次操作（RoundCount >= 活跃玩家数）
        if (State.RoundCount >= activePlayers.Count && activePlayers.Count >= 2 && player.Chips >= compareCost)
            actions.Add(ZjhAction.Compare);

        // 弃牌
        actions.Add(ZjhAction.Fold);

        // 全押
        if (player.Chips > 0)
            actions.Add(ZjhAction.AllIn);

        Console.WriteLine($"[ZjhGameEngine] GetAvailableActions: actions=[{string.Join(", ", actions)}]");
        return actions;
    }

    /// <summary>
    /// 获取游戏摘要（用于客户端展示）
    /// </summary>
    public object GetGameSummary()
    {
        return new
        {
            State.RoomId,
            State.Phase,
            State.Pot,
            State.BetAmount,
            State.RoundCount,
            CurrentPlayerId = State.CurrentPlayer?.UserId,
            State.WinnerId,
            Players = State.Players.Select(p => new
            {
                p.UserId,
                p.Username,
                p.Nickname,
                p.SeatIndex,
                p.Chips,
                p.TotalBet,
                p.HasLooked,
                p.IsFolded,
                p.IsOut,
                p.IsCompareLose,
                p.LastAction,
                Hand = p.UserId == State.WinnerId ? p.Hand : (p.HasLooked ? p.Hand : null)
            })
        };
    }
}
