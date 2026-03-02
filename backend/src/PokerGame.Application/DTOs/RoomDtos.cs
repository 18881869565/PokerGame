namespace PokerGame.Application.DTOs;

/// <summary>
/// 房间信息 DTO
/// </summary>
public class RoomDto
{
    /// <summary>
    /// 房间ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 6位房间号
    /// </summary>
    public string RoomCode { get; set; } = string.Empty;

    /// <summary>
    /// 房主ID
    /// </summary>
    public long OwnerId { get; set; }

    /// <summary>
    /// 房主昵称
    /// </summary>
    public string OwnerNickname { get; set; } = string.Empty;

    /// <summary>
    /// 最大玩家数
    /// </summary>
    public int MaxPlayers { get; set; }

    /// <summary>
    /// 小盲注
    /// </summary>
    public int SmallBlind { get; set; }

    /// <summary>
    /// 大盲注
    /// </summary>
    public int BigBlind { get; set; }

    /// <summary>
    /// 房间状态
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// 当前玩家列表
    /// </summary>
    public List<RoomPlayerDto> Players { get; set; } = new();
}

/// <summary>
/// 房间玩家 DTO
/// </summary>
public class RoomPlayerDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 昵称
    /// </summary>
    public string Nickname { get; set; } = string.Empty;

    /// <summary>
    /// 头像
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 座位号
    /// </summary>
    public int SeatIndex { get; set; }

    /// <summary>
    /// 带入筹码
    /// </summary>
    public long Chips { get; set; }

    /// <summary>
    /// 是否准备
    /// </summary>
    public bool IsReady { get; set; }

    /// <summary>
    /// 是否在线
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// 是否是房主
    /// </summary>
    public bool IsOwner { get; set; }
}

/// <summary>
/// 创建房间请求 DTO
/// </summary>
public class CreateRoomDto
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
/// 创建房间结果 DTO
/// </summary>
public class CreateRoomResultDto
{
    /// <summary>
    /// 房间ID
    /// </summary>
    public long RoomId { get; set; }

    /// <summary>
    /// 房间号
    /// </summary>
    public string RoomCode { get; set; } = string.Empty;
}
