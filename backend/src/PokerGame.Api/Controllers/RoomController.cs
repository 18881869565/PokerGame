using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokerGame.Api.Controllers;
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

    public RoomController(IRoomService roomService)
    {
        _roomService = roomService;
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
            BigBlind = request.BigBlind
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
