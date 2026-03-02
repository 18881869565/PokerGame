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
        }
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// 用户断开连接时
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // 清理连接映射
        if (_connectionRoomMap.TryGetValue(Context.ConnectionId, out var roomId))
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                await _gameService.HandlePlayerDisconnectAsync(roomId, userId.Value);

                // 通知其他玩家
                await Clients.Group($"Room_{roomId}").SendAsync("PlayerDisconnected", new
                {
                    UserId = userId.Value,
                    Timestamp = DateTime.Now
                });
            }

            _connectionRoomMap.Remove(Context.ConnectionId);
        }

        _connectionUserMap.Remove(Context.ConnectionId);
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
    }

    /// <summary>
    /// 离开房间
    /// </summary>
    public async Task LeaveRoom(string roomCode)
    {
        var userId = GetUserId();
        if (!userId.HasValue) return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Room_{roomCode}");
        _connectionRoomMap.Remove(Context.ConnectionId);

        // 通知房间内其他玩家
        await Clients.Group($"Room_{roomCode}").SendAsync("PlayerLeft", new
        {
            UserId = userId.Value,
            ConnectionId = Context.ConnectionId,
            Timestamp = DateTime.Now
        });
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
        if (!userId.HasValue) return;

        var room = await _roomService.GetByRoomCodeAsync(roomCode);
        if (room == null) return;

        var (success, message, gameState, eventType) = await _gameService.PlayerActionAsync(room.Id, userId.Value, action, amount);

        if (!success)
        {
            await Clients.Caller.SendAsync("Error", message);
            return;
        }

        // 向所有玩家发送更新后的游戏状态
        await SendGameStateToAllPlayers(room.Id, roomCode);

        // 如果游戏结束，发送结果
        if (eventType == GameEventType.GameEnded)
        {
            var result = _gameService.GetGameResult(room.Id);
            if (result != null)
            {
                await Clients.Group($"Room_{roomCode}").SendAsync("GameEnded", result);
            }
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
