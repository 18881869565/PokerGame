using System.Collections.Concurrent;
using PokerGame.Application.DTOs;
using PokerGame.Application.Interfaces;
using PokerGame.Domain.Entities;
using PokerGame.Domain.Enums;
using PokerGame.Domain.GameLogic;
using PokerGame.Infrastructure.Repository;

namespace PokerGame.Application.Services;

/// <summary>
/// 扎金花游戏服务实现
/// </summary>
public class ZjhGameService : IZjhGameService
{
    private readonly IRepository<Room> _roomRepository;
    private readonly IRepository<RoomPlayer> _roomPlayerRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Game> _gameRepository;

    // 静态字段：跨所有实例共享游戏状态
    private static readonly ConcurrentDictionary<long, ZjhGameEngine> _activeGames = new();
    private static readonly ConcurrentDictionary<long, ZjhGameResultDto> _gameResults = new();

    public ZjhGameService(
        IRepository<Room> roomRepository,
        IRepository<RoomPlayer> roomPlayerRepository,
        IRepository<User> userRepository,
        IRepository<Game> gameRepository)
    {
        _roomRepository = roomRepository;
        _roomPlayerRepository = roomPlayerRepository;
        _userRepository = userRepository;
        _gameRepository = gameRepository;
    }

    /// <summary>
    /// 开始游戏
    /// </summary>
    public async Task<(bool Success, string Message, ZjhGameStateDto? State)> StartGameAsync(long roomId, long userId)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null)
        {
            return (false, "房间不存在", null);
        }

        if (room.OwnerId != userId)
        {
            return (false, "只有房主才能开始游戏", null);
        }

        if (room.Status != RoomStatus.Waiting)
        {
            return (false, "游戏已在进行中", null);
        }

        var roomPlayers = await _roomPlayerRepository.GetListAsync(rp => rp.RoomId == roomId && rp.IsOnline);
        if (roomPlayers.Count < 2)
        {
            return (false, "至少需要2名玩家才能开始游戏", null);
        }

        // 检查是否所有人都准备好了
        if (roomPlayers.Any(rp => !rp.IsReady && rp.UserId != userId))
        {
            return (false, "还有玩家未准备", null);
        }

        // 创建游戏记录
        var game = new Game
        {
            RoomId = roomId,
            StartTime = DateTime.Now,
            Pot = 0
        };
        var gameId = await _gameRepository.InsertReturnIdentityAsync(game);

        // 创建游戏引擎
        var engine = ZjhGameEngine.Create(roomId, anteAmount: room.SmallBlind, maxRounds: 20);

        // 添加玩家
        foreach (var rp in roomPlayers)
        {
            var user = await _userRepository.GetByIdAsync(rp.UserId);
            if (user == null) continue;

            engine.AddPlayer(rp.UserId, user.Username, user.Nickname, rp.SeatIndex, rp.Chips);
        }

        // 开始游戏
        engine.StartGame();
        engine.State.GameId = gameId;

        // 保存到内存
        _activeGames[roomId] = engine;

        // 更新房间状态
        room.Status = RoomStatus.Playing;
        await _roomRepository.UpdateAsync(room);

        return (true, "游戏开始", MapToDto(engine.State, null));
    }

    /// <summary>
    /// 玩家看牌
    /// </summary>
    public Task<(bool Success, string Message, ZjhGameStateDto? State)> LookCardsAsync(long roomId, long userId)
    {
        if (!_activeGames.TryGetValue(roomId, out var engine))
        {
            return Task.FromResult<(bool, string, ZjhGameStateDto?)>((false, "游戏不存在", null));
        }

        try
        {
            engine.LookCards(userId);
            return Task.FromResult((true, "已看牌", MapToDto(engine.State, userId)));
        }
        catch (Exception ex)
        {
            return Task.FromResult<(bool, string, ZjhGameStateDto?)>((false, ex.Message, null));
        }
    }

    /// <summary>
    /// 玩家下注
    /// </summary>
    public async Task<(bool Success, string Message, ZjhGameStateDto? State)> BetAsync(long roomId, long userId)
    {
        if (!_activeGames.TryGetValue(roomId, out var engine))
        {
            return (false, "游戏不存在", null);
        }

        try
        {
            engine.Bet(userId);

            if (engine.State.Phase == ZjhGamePhase.Finished)
            {
                await FinishGameAsync(roomId, engine.State);
            }

            return (true, "下注成功", MapToDto(engine.State, userId));
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    /// <summary>
    /// 玩家加注
    /// </summary>
    public async Task<(bool Success, string Message, ZjhGameStateDto? State)> RaiseAsync(long roomId, long userId, long newBetAmount)
    {
        if (!_activeGames.TryGetValue(roomId, out var engine))
        {
            return (false, "游戏不存在", null);
        }

        try
        {
            engine.Raise(userId, newBetAmount);

            if (engine.State.Phase == ZjhGamePhase.Finished)
            {
                await FinishGameAsync(roomId, engine.State);
            }

            return (true, "加注成功", MapToDto(engine.State, userId));
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    /// <summary>
    /// 玩家比牌
    /// </summary>
    public async Task<(bool Success, string Message, ZjhGameStateDto? State, ZjhGameResultDto? Result, ZjhCompareResultDto? CompareResult)> CompareAsync(long roomId, long userId, long targetUserId)
    {
        if (!_activeGames.TryGetValue(roomId, out var engine))
        {
            return (false, "游戏不存在", null, null, null);
        }

        try
        {
            // 比牌前获取两个玩家的看牌状态
            var challenger = engine.State.GetPlayer(userId);
            var target = engine.State.GetPlayer(targetUserId);
            var challengerHadLooked = challenger?.HasLooked ?? false;
            var targetHadLooked = target?.HasLooked ?? false;

            engine.Compare(userId, targetUserId);

            ZjhGameResultDto? result = null;
            if (engine.State.Phase == ZjhGamePhase.Finished)
            {
                result = await FinishGameAsync(roomId, engine.State);
            }

            // 检查比牌输家是否需要看牌
            ZjhCompareResultDto? compareResult = null;
            var evaluator = new ZjhHandEvaluator();

            // 重新获取玩家状态（比牌后）
            challenger = engine.State.GetPlayer(userId);
            target = engine.State.GetPlayer(targetUserId);

            if (challenger?.IsCompareLose == true && !challengerHadLooked)
            {
                // 发起者输了且之前没看过牌
                compareResult = new ZjhCompareResultDto
                {
                    LoserId = userId,
                    LoserHasNotLooked = true,
                    LoserHand = new ZjhPlayerHandResultDto
                    {
                        UserId = challenger.UserId,
                        Nickname = challenger.Nickname,
                        Hand = challenger.Hand.Select(c => CardDto.FromCard(c)).ToList(),
                        HandDescription = evaluator.GetHandDescription(evaluator.Evaluate(challenger.Hand).Rank, challenger.Hand),
                        IsWinner = false
                    }
                };
            }
            else if (target?.IsCompareLose == true && !targetHadLooked)
            {
                // 目标玩家输了且之前没看过牌
                compareResult = new ZjhCompareResultDto
                {
                    LoserId = targetUserId,
                    LoserHasNotLooked = true,
                    LoserHand = new ZjhPlayerHandResultDto
                    {
                        UserId = target.UserId,
                        Nickname = target.Nickname,
                        Hand = target.Hand.Select(c => CardDto.FromCard(c)).ToList(),
                        HandDescription = evaluator.GetHandDescription(evaluator.Evaluate(target.Hand).Rank, target.Hand),
                        IsWinner = false
                    }
                };
            }

            return (true, "比牌完成", MapToDto(engine.State, userId), result, compareResult);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null, null, null);
        }
    }

    /// <summary>
    /// 玩家弃牌
    /// </summary>
    public async Task<(bool Success, string Message, ZjhGameStateDto? State, ZjhGameResultDto? Result)> FoldAsync(long roomId, long userId)
    {
        if (!_activeGames.TryGetValue(roomId, out var engine))
        {
            return (false, "游戏不存在", null, null);
        }

        try
        {
            engine.Fold(userId);

            ZjhGameResultDto? result = null;
            if (engine.State.Phase == ZjhGamePhase.Finished)
            {
                result = await FinishGameAsync(roomId, engine.State);
            }

            return (true, "已弃牌", MapToDto(engine.State, userId), result);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null, null);
        }
    }

    /// <summary>
    /// 玩家全押
    /// </summary>
    public async Task<(bool Success, string Message, ZjhGameStateDto? State)> AllInAsync(long roomId, long userId)
    {
        if (!_activeGames.TryGetValue(roomId, out var engine))
        {
            return (false, "游戏不存在", null);
        }

        try
        {
            engine.AllIn(userId);

            if (engine.State.Phase == ZjhGamePhase.Finished)
            {
                await FinishGameAsync(roomId, engine.State);
            }

            return (true, "全押成功", MapToDto(engine.State, userId));
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    /// <summary>
    /// 获取游戏状态
    /// </summary>
    public ZjhGameStateDto? GetGameState(long roomId)
    {
        if (!_activeGames.TryGetValue(roomId, out var engine))
        {
            return null;
        }
        return MapToDto(engine.State, null);
    }

    /// <summary>
    /// 获取玩家的游戏状态（包含自己的手牌）
    /// </summary>
    public ZjhGameStateDto? GetGameStateForPlayer(long roomId, long userId)
    {
        if (!_activeGames.TryGetValue(roomId, out var engine))
        {
            return null;
        }
        return MapToDto(engine.State, userId);
    }

    /// <summary>
    /// 获取玩家可用操作
    /// </summary>
    public ZjhAvailableActionsDto? GetAvailableActions(long roomId, long userId)
    {
        if (!_activeGames.TryGetValue(roomId, out var engine))
        {
            return null;
        }

        var actions = engine.GetAvailableActions(userId);
        var player = engine.State.GetPlayer(userId);

        return new ZjhAvailableActionsDto
        {
            Actions = actions,
            MinBetAmount = engine.State.BetAmount * (player?.BetMultiplier ?? 1),
            CompareCost = engine.State.BetAmount * (player?.BetMultiplier ?? 1) * 2
        };
    }

    /// <summary>
    /// 玩家断开连接处理
    /// </summary>
    public async Task HandlePlayerDisconnectAsync(long roomId, long userId)
    {
        if (!_activeGames.TryGetValue(roomId, out var engine))
        {
            return;
        }

        var player = engine.State.GetPlayer(userId);
        if (player == null) return;

        // 如果是当前操作玩家，自动弃牌
        if (engine.State.CurrentPlayer?.UserId == userId && engine.State.Phase == ZjhGamePhase.Betting)
        {
            try
            {
                engine.Fold(userId);

                if (engine.State.Phase == ZjhGamePhase.Finished)
                {
                    await FinishGameAsync(roomId, engine.State);
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// 检查房间是否有正在进行的游戏
    /// </summary>
    public bool HasActiveGame(long roomId)
    {
        return _activeGames.ContainsKey(roomId) && _activeGames[roomId].State.Phase != ZjhGamePhase.Finished;
    }

    /// <summary>
    /// 获取游戏结果
    /// </summary>
    public ZjhGameResultDto? GetGameResult(long roomId)
    {
        _gameResults.TryGetValue(roomId, out var result);
        return result;
    }

    /// <summary>
    /// 结束游戏并重置房间状态
    /// </summary>
    public async Task EndGameAndResetRoomAsync(long roomId)
    {
        _activeGames.TryRemove(roomId, out _);
        _gameResults.TryRemove(roomId, out _);

        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room != null && room.Status == RoomStatus.Playing)
        {
            room.Status = RoomStatus.Waiting;
            await _roomRepository.UpdateAsync(room);
        }
    }

    /// <summary>
    /// 游戏结束处理
    /// </summary>
    private async Task<ZjhGameResultDto> FinishGameAsync(long roomId, ZjhGameState state)
    {
        var evaluator = new ZjhHandEvaluator();

        // 更新游戏记录
        if (state.GameId.HasValue)
        {
            var game = await _gameRepository.GetByIdAsync(state.GameId.Value);
            if (game != null)
            {
                game.EndTime = DateTime.Now;
                game.Pot = state.Pot;
                game.WinnerId = state.WinnerId;
                await _gameRepository.UpdateAsync(game);
            }
        }

        // 更新玩家筹码
        var winner = state.GetPlayer(state.WinnerId ?? 0);
        var result = new ZjhGameResultDto
        {
            WinnerId = state.WinnerId ?? 0,
            WinnerNickname = winner?.Nickname ?? "",
            Pot = state.Pot,
            HandDescription = winner != null && winner.Hand.Any()
                ? evaluator.GetHandDescription(evaluator.Evaluate(winner.Hand).Rank, winner.Hand)
                : null
        };

        foreach (var player in state.Players)
        {
            var roomPlayer = await _roomPlayerRepository.FirstOrDefaultAsync(
                rp => rp.RoomId == roomId && rp.UserId == player.UserId);

            if (roomPlayer == null) continue;

            roomPlayer.Chips = player.Chips;
            await _roomPlayerRepository.UpdateAsync(roomPlayer);

            // 更新用户总筹码
            var user = await _userRepository.GetByIdAsync(player.UserId);
            if (user != null)
            {
                user.Chips = player.Chips;
                await _userRepository.UpdateAsync(user);
            }

            // 添加到手牌结果（游戏结束时显示还在游戏中或比牌输掉的玩家的手牌）
            // 条件：未弃牌且未出局，或者是比牌输掉（比牌输家也要展示牌）
            if ((!player.IsFolded && !player.IsOut) || player.IsCompareLose)
            {
                var handEval = player.Hand.Any() ? evaluator.Evaluate(player.Hand) : (Rank: ZjhHandRank.HighCard, Score: 0);
                result.PlayerHands.Add(new ZjhPlayerHandResultDto
                {
                    UserId = player.UserId,
                    Nickname = player.Nickname,
                    Hand = player.Hand.Select(c => CardDto.FromCard(c)).ToList(),
                    HandDescription = evaluator.GetHandDescription(handEval.Rank, player.Hand),
                    IsWinner = player.UserId == state.WinnerId
                });
            }
        }

        // 更新房间状态
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room != null)
        {
            room.Status = RoomStatus.Waiting;
            await _roomRepository.UpdateAsync(room);
        }

        // 保存结果
        _gameResults[roomId] = result;

        return result;
    }

    /// <summary>
    /// 映射到 DTO
    /// </summary>
    private static ZjhGameStateDto MapToDto(ZjhGameState state, long? forUserId)
    {
        return new ZjhGameStateDto
        {
            Phase = state.Phase,
            Pot = state.Pot,
            BetAmount = state.BetAmount,
            AnteAmount = state.AnteAmount,
            RoundCount = state.RoundCount,
            MaxRounds = state.MaxRounds,
            CurrentPlayerId = state.CurrentPlayer?.UserId,
            WinnerId = state.WinnerId,
            Players = state.Players.Select(p => new ZjhPlayerDto
            {
                UserId = p.UserId,
                Username = p.Username,
                Nickname = p.Nickname,
                SeatIndex = p.SeatIndex,
                Chips = p.Chips,
                TotalBet = p.TotalBet,
                HasLooked = p.HasLooked,
                IsFolded = p.IsFolded,
                IsOut = p.IsOut,
                IsCompareLose = p.IsCompareLose,
                LastAction = p.LastAction,
                IsOnline = true, // 游戏中的玩家默认在线
                // 只有看过的牌或者游戏结束时才能看到自己的牌
                Hand = (p.UserId == forUserId && p.HasLooked) || state.Phase == ZjhGamePhase.Finished
                    ? p.Hand.Select(c => CardDto.FromCard(c)).ToList()
                    : null
            }).ToList()
        };
    }
}
