using System.Collections.Concurrent;
using PokerGame.Application.DTOs;
using PokerGame.Application.Interfaces;
using PokerGame.Domain.Entities;
using PokerGame.Domain.Enums;
using PokerGame.Domain.GameLogic;
using PokerGame.Infrastructure.Repository;

namespace PokerGame.Application.Services;

/// <summary>
/// 游戏服务实现
/// </summary>
public class GameService : IGameService
{
    private readonly IRepository<Room> _roomRepository;
    private readonly IRepository<RoomPlayer> _roomPlayerRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Game> _gameRepository;

    // 静态字段：跨所有实例共享游戏状态（因为 GameService 是 Scoped）
    private static readonly ConcurrentDictionary<long, GameState> _activeGames = new();
    private static readonly PokerEngine _pokerEngine = new();
    // 存储需要被踢出的玩家（游戏结束后筹码不足）
    private static readonly ConcurrentDictionary<long, List<long>> _playersToRemove = new();

    public GameService(
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
    public async Task<(bool Success, string Message, GameStateDto? State)> StartGameAsync(long roomId, long userId)
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

        // 创建游戏玩家列表
        var gamePlayers = new List<GamePlayer>();
        foreach (var rp in roomPlayers)
        {
            var user = await _userRepository.GetByIdAsync(rp.UserId);
            if (user == null) continue;

            gamePlayers.Add(new GamePlayer
            {
                UserId = user.Id,
                Username = user.Username,
                Nickname = user.Nickname,
                SeatIndex = rp.SeatIndex,
                Chips = rp.Chips
            });
        }

        // 创建游戏记录
        var game = new Domain.Entities.Game
        {
            RoomId = roomId,
            StartTime = DateTime.Now,
            Pot = 0
        };
        var gameId = await _gameRepository.InsertReturnIdentityAsync(game);

        // 开始游戏
        var gameState = _pokerEngine.StartNewGame(gamePlayers, room.SmallBlind, room.BigBlind);
        gameState.RoomId = roomId;
        gameState.GameId = gameId;

        // 保存到内存
        _activeGames[roomId] = gameState;

        // 更新房间状态
        room.Status = RoomStatus.Playing;
        await _roomRepository.UpdateAsync(room);

        return (true, "游戏开始", MapToDto(gameState, null));
    }

    /// <summary>
    /// 玩家操作
    /// </summary>
    public async Task<(bool Success, string Message, GameStateDto? State, GameEventType EventType)> PlayerActionAsync(
        long roomId, long userId, PlayerAction action, long amount = 0)
    {
        if (!_activeGames.TryGetValue(roomId, out var gameState))
        {
            return (false, "游戏不存在", null, GameEventType.GameEnded);
        }

        if (gameState.Phase == GamePhase.Finished)
        {
            return (false, "游戏已结束", null, GameEventType.GameEnded);
        }

        if (gameState.CurrentPlayer?.UserId != userId)
        {
            return (false, "还没轮到你操作", null, GameEventType.TurnChanged);
        }

        GameStateResult result;

        switch (action)
        {
            case PlayerAction.Fold:
                result = _pokerEngine.Fold(gameState, userId);
                break;

            case PlayerAction.Check:
                result = _pokerEngine.Check(gameState, userId);
                break;

            case PlayerAction.Call:
                result = _pokerEngine.Call(gameState, userId);
                break;

            case PlayerAction.Raise:
                result = _pokerEngine.Raise(gameState, userId, amount);
                break;

            case PlayerAction.AllIn:
                result = _pokerEngine.AllIn(gameState, userId);
                break;

            default:
                return (false, "未知操作", null, GameEventType.TurnChanged);
        }

        if (!result.IsSuccess)
        {
            return (false, result.Message, null, GameEventType.TurnChanged);
        }

        // 如果游戏结束，处理结算
        if (gameState.Phase == GamePhase.Finished)
        {
            var playersToRemove = await FinishGameAsync(roomId, gameState);
            // 保存需要踢出的玩家列表
            if (playersToRemove.Count > 0)
            {
                _playersToRemove[roomId] = playersToRemove;
            }
        }

        return (true, result.Message, MapToDto(gameState, userId), result.EventType);
    }

    /// <summary>
    /// 获取游戏状态
    /// </summary>
    public GameStateDto? GetGameState(long roomId)
    {
        if (!_activeGames.TryGetValue(roomId, out var gameState))
        {
            return null;
        }
        return MapToDto(gameState, null);
    }

    /// <summary>
    /// 获取玩家的游戏状态
    /// </summary>
    public GameStateDto? GetGameStateForPlayer(long roomId, long userId)
    {
        if (!_activeGames.TryGetValue(roomId, out var gameState))
        {
            return null;
        }
        return MapToDto(gameState, userId);
    }

    /// <summary>
    /// 玩家断开连接处理
    /// </summary>
    public async Task HandlePlayerDisconnectAsync(long roomId, long userId)
    {
        if (!_activeGames.TryGetValue(roomId, out var gameState))
        {
            return;
        }

        var player = gameState.GetPlayer(userId);
        if (player == null) return;

        // 如果是当前操作玩家，自动弃牌
        if (gameState.CurrentPlayer?.UserId == userId && gameState.Phase != GamePhase.Finished)
        {
            _pokerEngine.Fold(gameState, userId);
        }

        // 标记玩家为弃牌
        player.Status = PlayerStatus.Folded;

        // 检查是否只剩一个玩家
        var activePlayers = gameState.GetActivePlayers();
        if (activePlayers.Count == 1 && gameState.Phase != GamePhase.Finished)
        {
            gameState.Phase = GamePhase.Finished;
            await FinishGameAsync(roomId, gameState);
        }
    }

    /// <summary>
    /// 检查房间是否有正在进行的游戏
    /// </summary>
    public bool HasActiveGame(long roomId)
    {
        return _activeGames.ContainsKey(roomId) && _activeGames[roomId].Phase != GamePhase.Finished;
    }

    /// <summary>
    /// 获取游戏结果
    /// </summary>
    public GameResultDto? GetGameResult(long roomId)
    {
        if (!_activeGames.TryGetValue(roomId, out var gameState) || gameState.Phase != GamePhase.Finished)
        {
            return null;
        }

        var result = _pokerEngine.GetGameResult(gameState);

        return new GameResultDto
        {
            WinnerIds = result.Winners,
            Pot = result.Pot,
            PlayerHands = result.PlayerHands.Select(ph => new PlayerHandResultDto
            {
                UserId = ph.UserId,
                Nickname = gameState.Players.First(p => p.UserId == ph.UserId).Nickname,
                HoleCards = ph.HoleCards.Select(c => CardDto.FromCard(c)).ToList(),
                HandDescription = ph.Evaluation?.Description,
                ChipsWon = ph.ChipsWon
            }).ToList()
        };
    }

    /// <summary>
    /// 游戏结束处理
    /// 返回需要被踢出的玩家列表（筹码不足）
    /// </summary>
    private async Task<List<long>> FinishGameAsync(long roomId, GameState gameState)
    {
        var result = _pokerEngine.GetGameResult(gameState);
        var playersToRemove = new List<long>();

        // 获取房间信息（用于计算最低筹码）
        var room = await _roomRepository.GetByIdAsync(roomId);
        var minChips = room?.BigBlind * 50L ?? 1000L; // 最低入场筹码 = 50 个大盲

        // 更新游戏记录
        if (gameState.GameId.HasValue)
        {
            var game = await _gameRepository.GetByIdAsync(gameState.GameId.Value);
            if (game != null)
            {
                game.EndTime = DateTime.Now;
                game.Pot = gameState.Pot;
                game.WinnerId = result.Winners.FirstOrDefault();
                await _gameRepository.UpdateAsync(game);
            }
        }

        // 更新玩家筹码
        foreach (var playerResult in result.PlayerHands)
        {
            var player = gameState.GetPlayer(playerResult.UserId);
            if (player == null) continue;

            var roomPlayer = await _roomPlayerRepository.FirstOrDefaultAsync(
                rp => rp.RoomId == roomId && rp.UserId == playerResult.UserId);

            if (roomPlayer == null) continue;

            // 保存本局带入的原始筹码（更新前）
            var originalRoomChips = roomPlayer.Chips;

            // 计算本局结束后的筹码（确保不为负数）
            var finalChips = Math.Max(0, player.Chips + playerResult.ChipsWon);

            // 更新房间玩家筹码
            roomPlayer.Chips = finalChips;
            await _roomPlayerRepository.UpdateAsync(roomPlayer);

            // 更新用户总筹码
            var user = await _userRepository.GetByIdAsync(playerResult.UserId);
            if (user != null)
            {
                // 用户筹码 = 用户原有筹码 - 本局带入的筹码 + 本局结束后的筹码
                var userFinalChips = Math.Max(0, user.Chips - originalRoomChips + finalChips);
                user.Chips = userFinalChips;
                await _userRepository.UpdateAsync(user);
            }

            // 检查筹码是否低于最低入场筹码（房主除外）
            if (finalChips < minChips && room != null && room.OwnerId != playerResult.UserId)
            {
                playersToRemove.Add(playerResult.UserId);
            }
        }

        // 更新房间状态
        if (room != null)
        {
            room.Status = RoomStatus.Waiting;
            await _roomRepository.UpdateAsync(room);
        }

        return playersToRemove;
    }

    /// <summary>
    /// 映射到 DTO
    /// </summary>
    private static GameStateDto MapToDto(GameState state, long? forUserId)
    {
        return new GameStateDto
        {
            Phase = state.Phase,
            CommunityCards = state.CommunityCards.Select(c => CardDto.FromCard(c)).ToList(),
            Players = state.Players.Select(p => new GamePlayerDto
            {
                UserId = p.UserId,
                Nickname = p.Nickname,
                SeatIndex = p.SeatIndex,
                Chips = p.Chips,
                CurrentBet = p.CurrentBet,
                Status = p.Status,
                LastAction = p.LastAction,
                IsDealer = p.IsDealer,
                IsSmallBlind = p.IsSmallBlind,
                IsBigBlind = p.IsBigBlind,
                // 只有当前玩家才能看到自己的底牌
                HoleCards = p.UserId == forUserId && p.HoleCards.Any()
                    ? p.HoleCards.Select(c => CardDto.FromCard(c)).ToList()
                    : null
            }).ToList(),
            Pot = state.Pot,
            CurrentHighestBet = state.CurrentHighestBet,
            CurrentPlayerId = state.CurrentPlayer?.UserId,
            DealerId = state.Players.ElementAtOrDefault(state.DealerIndex)?.UserId ?? 0,
            SmallBlind = state.SmallBlind,
            BigBlind = state.BigBlind,
            // 检查是否有玩家已全押
            HasAllInPlayer = state.Players.Any(p => p.Status == PlayerStatus.AllIn)
        };
    }

    /// <summary>
    /// 获取并清除需要踢出的玩家列表（筹码不足）
    /// </summary>
    public List<long> GetAndClearPlayersToRemove(long roomId)
    {
        if (_playersToRemove.TryRemove(roomId, out var players))
        {
            return players;
        }
        return new List<long>();
    }

    /// <summary>
    /// 结束游戏并重置房间状态（用于游戏状态丢失的情况）
    /// </summary>
    public async Task EndGameAndResetRoomAsync(long roomId)
    {
        // 从内存中移除游戏状态（如果存在）
        _activeGames.TryRemove(roomId, out _);

        // 更新房间状态为等待中
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room != null && room.Status == RoomStatus.Playing)
        {
            room.Status = RoomStatus.Waiting;
            await _roomRepository.UpdateAsync(room);
        }
    }
}
