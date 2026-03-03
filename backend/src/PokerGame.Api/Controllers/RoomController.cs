using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PokerGame.Api.Controllers;
using PokerGame.Api.Hubs;
using PokerGame.Application.Interfaces;
using PokerGame.Application.DTOs;

namespace PokerGame.Api.Controllers;

/// <summary>
/// 房间控制器
/// </summary>
[Authorize]
[Route("api/room")]
public class RoomController : BaseController
{
    private readonly IRoomService _roomService;
    private readonly IHubContext<GameHub> _hubContext;

    public RoomController(IRoomService roomService, IHubContext<GameHub> hubContext)
    {
        _roomService = roomService;
        _hubContext = hubContext;
    }

    /// <summary>
    /// 创建房间
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
    {
        var dto = new CreateRoomDto
        {
            MaxPlayers = request.MaxPlayers,
            SmallBlind = request.SmallBlind,
            BigBlind = request.BigBlind,
            BringChips = request.BringChips
        };

        var (success, message, result) = await _roomService.CreateRoomAsync(CurrentUserId, dto);

        if (!success)
        {
            return Fail(message);
        }

        return Success(new { result!.RoomId, result.RoomCode }, message);
    }

    /// <summary>
    /// 获取房间信息
    /// </summary>
    [HttpGet("{roomCode}")]
    public async Task<IActionResult> GetRoom(string roomCode)
    {
        var room = await _roomService.GetByRoomCodeAsync(roomCode);
        if (room == null)
        {
            return Fail("房间不存在");
        }

        return Success(room);
    }

    /// <summary>
    /// 加入房间
    /// </summary>
    [HttpPost("join")]
    public async Task<IActionResult> JoinRoom([FromBody] JoinRoomRequest request)
    {
        var (success, message) = await _roomService.JoinRoomAsync(CurrentUserId, request.RoomCode, request.BringChips);

        if (!success)
        {
            return Fail(message);
        }

        return Success(message);
    }

    /// <summary>
    /// 离开房间
    /// </summary>
    [HttpPost("{roomId}/leave")]
    public async Task<IActionResult> LeaveRoom(long roomId)
    {
        var (success, message) = await _roomService.LeaveRoomAsync(CurrentUserId, roomId);

        if (!success)
        {
            return Fail(message);
        }

        return Success(message);
    }

    /// <summary>
    /// 准备/取消准备
    /// </summary>
    [HttpPost("{roomId}/ready")]
    public async Task<IActionResult> ToggleReady(long roomId)
    {
        var (success, message) = await _roomService.ToggleReadyAsync(CurrentUserId, roomId);

        if (!success)
        {
            return Fail(message);
        }

        return Success(message);
    }

    /// <summary>
    /// 踢出玩家
    /// </summary>
    [HttpPost("{roomId}/kick/{targetUserId}")]
    public async Task<IActionResult> KickPlayer(long roomId, long targetUserId)
    {
        var (success, message) = await _roomService.KickPlayerAsync(CurrentUserId, roomId, targetUserId);

        if (!success)
        {
            return Fail(message);
        }

        return Success(message);
    }

    /// <summary>
    /// 解散房间
    /// </summary>
    [HttpPost("{roomId}/dismiss")]
    public async Task<IActionResult> DismissRoom(long roomId)
    {
        var (success, message) = await _roomService.DismissRoomAsync(CurrentUserId, roomId);

        if (!success)
        {
            return Fail(message);
        }

        return Success(message);
    }

    /// <summary>
    /// 换位置
    /// </summary>
    [HttpPost("{roomId}/changeseat")]
    public async Task<IActionResult> ChangeSeat(long roomId, [FromBody] ChangeSeatRequest request)
    {
        var (success, message) = await _roomService.ChangeSeatAsync(CurrentUserId, roomId, request.SeatIndex);

        if (!success)
        {
            return Fail(message);
        }

        // 获取房间信息以获取 roomCode
        var room = await _roomService.GetByIdAsync(roomId);
        if (room != null)
        {
            // 通知房间内所有玩家座位变更
            await _hubContext.Clients.Group($"Room_{room.RoomCode}").SendAsync("SeatChanged", new
            {
                UserId = CurrentUserId,
                SeatIndex = request.SeatIndex,
                Timestamp = DateTime.Now
            });
        }

        return Success(message);
    }

    /// <summary>
    /// 生成房间二维码
    /// </summary>
    [HttpGet("qrcode/{roomCode}")]
    public async Task<IActionResult> GenerateQRCode(string roomCode)
    {
        // TODO: 实现真正的二维码生成
        var request = HttpContext.Request;
        var baseUrl = $"{request.Scheme}://{request.Host}";
        await Task.CompletedTask;
        return Success(new { QRCodeUrl = $"{baseUrl}/room/{roomCode}", RoomCode = roomCode });
    }
}

/// <summary>
/// 创建房间请求
/// </summary>
public class CreateRoomRequest
{
    /// <summary>
    /// 最大玩家数
    /// </summary>
    public int MaxPlayers { get; set; } = 9;

    /// <summary>
    /// 小盲注
    /// </summary>
    public int SmallBlind { get; set; } = 10;

    /// <summary>
    /// 大盲注
    /// </summary>
    public int BigBlind { get; set; } = 20;

    /// <summary>
    /// 带入筹码（0表示默认）
    /// </summary>
    public long BringChips { get; set; } = 0;
}

/// <summary>
/// 加入房间请求
/// </summary>
public class JoinRoomRequest
{
    /// <summary>
    /// 房间号
    /// </summary>
    public string RoomCode { get; set; } = string.Empty;

    /// <summary>
    /// 带入筹码（0表示默认）
    /// </summary>
    public long BringChips { get; set; } = 0;
}

/// <summary>
/// 换位置请求
/// </summary>
public class ChangeSeatRequest
{
    /// <summary>
    /// 目标座位号
    /// </summary>
    public int SeatIndex { get; set; }
}
