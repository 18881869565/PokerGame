using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PokerGame.Application.DTOs;
using PokerGame.Application.Interfaces;
using PokerGame.Domain.Enums;
using PokerGame.Domain.GameLogic;

namespace PokerGame.Api.Hubs;

/// <summary>
/// 游戏实时通信 Hub
/// </summary>
[Authorize]
public class GameHub : Hub
{
    private readonly IGameService _gameService;
    private readonly IRoomService _roomService;
    private static readonly Dictionary<string, long> _connectionUserMap = new();
    private static readonly Dictionary<string, long> _connectionRoomMap = new();
    // 跟踪每个用户的连接数
    private static readonly Dictionary<long, HashSet<string>> _userConnectionsMap = new();
    private static readonly object _lock = new();

    public GameHub(IGameService gameService, IRoomService roomService)
    {
        _gameService = gameService;
        _roomService = roomService;
    }

    /// <summary>
    /// 用户连接时
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            _connectionUserMap[Context.ConnectionId] = userId.Value;

            // 添加到用户连接映射
            lock (_lock)
            {
                if (!_userConnectionsMap.ContainsKey(userId.Value))
                {
                    _userConnectionsMap[userId.Value] = new HashSet<string>();
                }
                _userConnectionsMap[userId.Value].Add(Context.ConnectionId);
            }
        }
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// 用户断开连接时
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();

        // 清理连接映射
        if (_connectionRoomMap.TryGetValue(Context.ConnectionId, out var roomId))
        {
            _connectionRoomMap.Remove(Context.ConnectionId);
        }

        _connectionUserMap.Remove(Context.ConnectionId);

        // 从用户连接映射中移除
        bool hasOtherConnections = false;
        if (userId.HasValue)
        {
            lock (_lock)
            {
                if (_userConnectionsMap.ContainsKey(userId.Value))
                {
                    _userConnectionsMap[userId.Value].Remove(Context.ConnectionId);
                    hasOtherConnections = _userConnectionsMap[userId.Value].Count > 0;

                    // 如果没有其他连接，清理映射
                    if (!hasOtherConnections)
                    {
                        _userConnectionsMap.Remove(userId.Value);
                    }
                }
            }

            // 只有当用户没有其他连接时，才处理游戏断开
            if (!hasOtherConnections && roomId != 0)
            {
                await _gameService.HandlePlayerDisconnectAsync(roomId, userId.Value);

                // 通知其他玩家
                await Clients.Group($"Room_{roomId}").SendAsync("PlayerDisconnected", new
                {
                    UserId = userId.Value,
                    Timestamp = DateTime.Now
                });
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// 加入房间
    /// </summary>
    public async Task JoinRoom(string roomCode)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return;

        var room = await _roomService.GetByRoomCodeAsync(roomCode);
        if (room == null)
        {
            await Clients.Caller.SendAsync("Error", "房间不存在");
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"Room_{roomCode}");
        _connectionRoomMap[Context.ConnectionId] = room.Id;

        // 通知房间内其他玩家
        await Clients.Group($"Room_{roomCode}").SendAsync("PlayerJoined", new
        {
            UserId = userId.Value,
            ConnectionId = Context.ConnectionId,
            Timestamp = DateTime.Now
        });

        // 如果有正在进行的游戏，发送游戏状态
        var gameState = _gameService.GetGameStateForPlayer(room.Id, userId.Value);
        if (gameState != null)
        {
            await Clients.Caller.SendAsync("GameStateUpdated", gameState);
        }
        // 只有当房间确实在游戏中且状态丢失时，才重置（避免误判）
        // 检查是否有已入座的玩家（说明游戏可能正在进行）
        else if (room.Status == (int)Domain.Enums.RoomStatus.Playing)
        {
            // 检查房间是否有已入座的玩家在等待游戏恢复
            var roomPlayers = await _roomService.GetRoomPlayersAsync(room.Id);
            var seatedPlayers = roomPlayers.Where(p => p.SeatIndex >= 0).ToList();

            if (seatedPlayers.Count >= 2)
            {
                // 有已入座玩家，说明游戏确实在进行中，状态丢失了
                // 自动结束游戏并重置房间状态
                await _gameService.EndGameAndResetRoomAsync(room.Id);
                await Clients.Group($"Room_{roomCode}").SendAsync("GameReset", new
                {
                    Message = "游戏状态丢失，请重新开始游戏",
                    RoomCode = roomCode
                });
            }
            else
            {
                // 没有足够入座玩家，可能是游戏结束了但状态没更新，直接重置房间状态
                await _gameService.EndGameAndResetRoomAsync(room.Id);
                // 不发送 GameReset，因为这不需要中断玩家
            }
        }
    }

    /// <summary>
    /// 离开房间
    /// </summary>
    public async Task LeaveRoom(string roomCode)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return;

        var room = await _roomService.GetByRoomCodeAsync(roomCode);
        if (room == null) return;

        var isOwner = room.OwnerId == userId.Value;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Room_{roomCode}");
        _connectionRoomMap.Remove(Context.ConnectionId);

        // 调用服务层离开房间（更新数据库）
        var (success, message) = await _roomService.LeaveRoomAsync(userId.Value, room.Id);

        if (isOwner)
        {
            // 房主离开，通知所有玩家房间已解散
            await Clients.Group($"Room_{roomCode}").SendAsync("RoomDismissed", new
            {
                Message = "房主已解散房间",
                RoomCode = roomCode,
                Timestamp = DateTime.Now
            });
        }
        else
        {
            // 普通玩家离开，通知房间内其他玩家
            await Clients.Group($"Room_{roomCode}").SendAsync("PlayerLeft", new
            {
                UserId = userId.Value,
                ConnectionId = Context.ConnectionId,
                Timestamp = DateTime.Now
            });
        }
    }

    /// <summary>
    /// 准备游戏
    /// </summary>
    public async Task ReadyGame(string roomCode)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return;

        var room = await _roomService.GetByRoomCodeAsync(roomCode);
        if (room == null) return;

        var (success, message) = await _roomService.ToggleReadyAsync(userId.Value, room.Id);

        await Clients.Group($"Room_{roomCode}").SendAsync("PlayerReady", new
        {
            UserId = userId.Value,
            Success = success,
            Message = message,
            Timestamp = DateTime.Now
        });
    }

    /// <summary>
    /// 开始游戏（仅房主）
    /// </summary>
    public async Task StartGame(string roomCode)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return;

        var room = await _roomService.GetByRoomCodeAsync(roomCode);
        if (room == null)
        {
            await Clients.Caller.SendAsync("Error", "房间不存在");
            return;
        }

        var (success, message, gameState) = await _gameService.StartGameAsync(room.Id, userId.Value);

        if (!success)
        {
            await Clients.Caller.SendAsync("Error", message);
            return;
        }

        // 向所有玩家发送游戏状态
        await SendGameStateToAllPlayers(room.Id, roomCode);
        await Clients.Group($"Room_{roomCode}").SendAsync("GameStarted", new { Message = message });
    }

    /// <summary>
    /// 玩家操作
    /// </summary>
    public async Task DoAction(string roomCode, Domain.Enums.PlayerAction action, long amount = 0)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            await Clients.Caller.SendAsync("Error", "请先登录");
            return;
        }

        var room = await _roomService.GetByRoomCodeAsync(roomCode);
        if (room == null)
        {
            await Clients.Caller.SendAsync("Error", "房间不存在");
            return;
        }

        var (success, message, gameState, eventType) = await _gameService.PlayerActionAsync(room.Id, userId.Value, action, amount);

        if (!success)
        {
            await Clients.Caller.SendAsync("Error", message);
            return;
        }

        // 向所有玩家发送更新后的游戏状态
        await SendGameStateToAllPlayers(room.Id, roomCode);

        // 如果游戏结束（包括玩家获胜），发送结果
        if (eventType == GameEventType.GameEnded || eventType == GameEventType.PlayerWins)
        {
            var result = _gameService.GetGameResult(room.Id);
            if (result != null)
            {
                await Clients.Group($"Room_{roomCode}").SendAsync("GameEnded", result);
            }

            // 处理筹码不足的玩家，自动踢出
            var playersToRemove = _gameService.GetAndClearPlayersToRemove(room.Id);
            foreach (var playerUserId in playersToRemove)
            {
                // 踢出玩家
                await _roomService.LeaveRoomAsync(playerUserId, room.Id);

                // 通知被踢出的玩家
                await Clients.Group($"Room_{roomCode}").SendAsync("PlayerRemoved", new
                {
                    UserId = playerUserId,
                    Reason = "筹码不足，已自动离开房间",
                    Timestamp = DateTime.Now
                });
            }

            // 发送更新后的房间玩家列表
            var updatedPlayers = await _roomService.GetRoomPlayersAsync(room.Id);
            await Clients.Group($"Room_{roomCode}").SendAsync("RoomPlayersUpdated", updatedPlayers);
        }
    }

    /// <summary>
    /// 下注
    /// </summary>
    public Task Bet(string roomCode, long amount) => DoAction(roomCode, Domain.Enums.PlayerAction.Call, amount);

    /// <summary>
    /// 弃牌
    /// </summary>
    public Task Fold(string roomCode) => DoAction(roomCode, Domain.Enums.PlayerAction.Fold);

    /// <summary>
    /// 过牌
    /// </summary>
    public Task Check(string roomCode) => DoAction(roomCode, Domain.Enums.PlayerAction.Check);

    /// <summary>
    /// 加注
    /// </summary>
    public Task Raise(string roomCode, long amount) => DoAction(roomCode, Domain.Enums.PlayerAction.Raise, amount);

    /// <summary>
    /// 全押
    /// </summary>
    public Task AllIn(string roomCode) => DoAction(roomCode, Domain.Enums.PlayerAction.AllIn);

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    private long? GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst("sub")?.Value ?? Context.User?.FindFirst("id")?.Value;
        return long.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// 向所有玩家发送游戏状态（每人看到自己的底牌）
    /// </summary>
    private async Task SendGameStateToAllPlayers(long roomId, string roomCode)
    {
        // 获取房间内所有连接
        var roomConnections = _connectionRoomMap
            .Where(kvp => kvp.Value == roomId)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var connectionId in roomConnections)
        {
            if (_connectionUserMap.TryGetValue(connectionId, out var userId))
            {
                var state = _gameService.GetGameStateForPlayer(roomId, userId);
                if (state != null)
                {
                    await Clients.Client(connectionId).SendAsync("GameStateUpdated", state);
                }
            }
        }
    }
}
