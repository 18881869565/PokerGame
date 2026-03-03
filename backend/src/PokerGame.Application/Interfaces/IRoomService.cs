using PokerGame.Application.DTOs;

namespace PokerGame.Application.Interfaces;

/// <summary>
/// 房间服务接口
/// </summary>
public interface IRoomService
{
    /// <summary>
    /// 创建房间
    /// </summary>
    Task<(bool Success, string Message, CreateRoomResultDto? Result)> CreateRoomAsync(long userId, CreateRoomDto dto);

    /// <summary>
    /// 根据房间号获取房间信息
    /// </summary>
    Task<RoomDto?> GetByRoomCodeAsync(string roomCode);

    /// <summary>
    /// 根据ID获取房间信息
    /// </summary>
    Task<RoomDto?> GetByIdAsync(long roomId);

    /// <summary>
    /// 加入房间
    /// </summary>
    Task<(bool Success, string Message)> JoinRoomAsync(long userId, string roomCode, long bringChips);

    /// <summary>
    /// 离开房间
    /// </summary>
    Task<(bool Success, string Message)> LeaveRoomAsync(long userId, long roomId);

    /// <summary>
    /// 准备/取消准备
    /// </summary>
    Task<(bool Success, string Message)> ToggleReadyAsync(long userId, long roomId);

    /// <summary>
    /// 踢出玩家（房主权限）
    /// </summary>
    Task<(bool Success, string Message)> KickPlayerAsync(long ownerId, long roomId, long targetUserId);

    /// <summary>
    /// 解散房间（房主权限）
    /// </summary>
    Task<(bool Success, string Message)> DismissRoomAsync(long ownerId, long roomId);

    /// <summary>
    /// 获取房间内的玩家列表
    /// </summary>
    Task<List<RoomPlayerDto>> GetRoomPlayersAsync(long roomId);

    /// <summary>
    /// 换位置
    /// </summary>
    Task<(bool Success, string Message)> ChangeSeatAsync(long userId, long roomId, int newSeatIndex);
}
