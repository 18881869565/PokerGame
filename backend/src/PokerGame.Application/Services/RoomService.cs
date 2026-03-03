using PokerGame.Application.DTOs;
using PokerGame.Application.Interfaces;
using PokerGame.Domain.Entities;
using PokerGame.Domain.Enums;
using PokerGame.Infrastructure.Repository;

namespace PokerGame.Application.Services;

/// <summary>
/// 房间服务实现
/// </summary>
public class RoomService : IRoomService
{
    private readonly IRepository<Room> _roomRepository;
    private readonly IRepository<RoomPlayer> _roomPlayerRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IGameService _gameService;

    public RoomService(
        IRepository<Room> roomRepository,
        IRepository<RoomPlayer> roomPlayerRepository,
        IRepository<User> userRepository,
        IGameService gameService)
    {
        _roomRepository = roomRepository;
        _roomPlayerRepository = roomPlayerRepository;
        _userRepository = userRepository;
        _gameService = gameService;
    }

    /// <summary>
    /// 创建房间
    /// </summary>
    public async Task<(bool Success, string Message, CreateRoomResultDto? Result)> CreateRoomAsync(long userId, CreateRoomDto dto)
    {
        // 验证参数
        if (dto.MaxPlayers < 2 || dto.MaxPlayers > 9)
        {
            return (false, "玩家数量必须在2-9之间", null);
        }

        if (dto.BigBlind <= 0 || dto.SmallBlind <= 0)
        {
            return (false, "盲注必须大于0", null);
        }

        if (dto.SmallBlind >= dto.BigBlind)
        {
            return (false, "小盲注必须小于大盲注", null);
        }

        // 检查用户是否已在其他房间，自动离开旧房间
        var existingPlayer = await _roomPlayerRepository.FirstOrDefaultAsync(rp => rp.UserId == userId && rp.IsOnline);
        if (existingPlayer != null)
        {
            // 自动离开旧房间
            existingPlayer.IsOnline = false;
            await _roomPlayerRepository.UpdateAsync(existingPlayer);
        }

        // 生成房间号
        var roomCode = await GenerateRoomCodeAsync();

        // 创建房间
        var room = new Room
        {
            RoomCode = roomCode,
            OwnerId = userId,
            MaxPlayers = dto.MaxPlayers,
            SmallBlind = dto.SmallBlind,
            BigBlind = dto.BigBlind,
            Status = RoomStatus.Waiting,
            CreatedAt = DateTime.Now
        };

        var roomId = await _roomRepository.InsertReturnIdentityAsync(room);
        room.Id = roomId;

        // 房主自动加入房间
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return (false, "用户不存在", null);
        }

        // 计算最低入场筹码（50个大盲）
        var minChips = room.BigBlind * 50L;

        // 确定带入筹码
        long bringChips;
        if (dto.BringChips > 0)
        {
            // 指定了筹码，验证其有效性
            if (dto.BringChips < minChips)
            {
                return (false, $"最低入场筹码为{minChips}", null);
            }
            bringChips = dto.BringChips;
        }
        else
        {
            // 未指定筹码，使用默认值
            bringChips = Math.Min(user.Chips, minChips);
        }

        if (user.Chips < bringChips)
        {
            return (false, "筹码不足", null);
        }

        var roomPlayer = new RoomPlayer
        {
            RoomId = roomId,
            UserId = userId,
            SeatIndex = 0,
            Chips = bringChips,
            IsReady = true, // 房主默认准备
            IsOnline = true
        };

        await _roomPlayerRepository.AddAsync(roomPlayer);

        return (true, "房间创建成功", new CreateRoomResultDto
        {
            RoomId = roomId,
            RoomCode = roomCode
        });
    }

    /// <summary>
    /// 根据房间号获取房间信息
    /// </summary>
    public async Task<RoomDto?> GetByRoomCodeAsync(string roomCode)
    {
        var room = await _roomRepository.FirstOrDefaultAsync(r => r.RoomCode == roomCode);
        if (room == null) return null;

        return await MapToDtoAsync(room);
    }

    /// <summary>
    /// 根据ID获取房间信息
    /// </summary>
    public async Task<RoomDto?> GetByIdAsync(long roomId)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null) return null;

        return await MapToDtoAsync(room);
    }

    /// <summary>
    /// 加入房间
    /// </summary>
    public async Task<(bool Success, string Message)> JoinRoomAsync(long userId, string roomCode, long bringChips)
    {
        var room = await _roomRepository.FirstOrDefaultAsync(r => r.RoomCode == roomCode);
        if (room == null)
        {
            return (false, "房间不存在");
        }

        if (room.Status != RoomStatus.Waiting)
        {
            // 检查是否真的有活跃游戏
            if (_gameService.HasActiveGame(room.Id))
            {
                return (false, "房间正在游戏中，无法加入");
            }

            // 没有活跃游戏，自动重置房间状态
            room.Status = RoomStatus.Waiting;
            await _roomRepository.UpdateAsync(room);
        }

        // 检查并自动退出其他房间
        var otherRoomPlayers = await _roomPlayerRepository.GetListAsync(rp => rp.UserId == userId && rp.RoomId != room.Id && rp.IsOnline);
        foreach (var otherPlayer in otherRoomPlayers)
        {
            otherPlayer.IsOnline = false;
            await _roomPlayerRepository.UpdateAsync(otherPlayer);
        }

        // 检查是否已在当前房间中
        var existingPlayer = await _roomPlayerRepository.FirstOrDefaultAsync(rp => rp.RoomId == room.Id && rp.UserId == userId);
        if (existingPlayer != null)
        {
            // 重新上线
            existingPlayer.IsOnline = true;
            await _roomPlayerRepository.UpdateAsync(existingPlayer);
            return (true, "重新加入房间成功");
        }

        // 检查房间人数
        var playerCount = await _roomPlayerRepository.CountAsync(rp => rp.RoomId == room.Id && rp.IsOnline);
        if (playerCount >= room.MaxPlayers)
        {
            return (false, "房间已满");
        }

        // 检查用户筹码
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return (false, "用户不存在");
        }

        // 计算最低入场筹码（50个大盲）
        var minChips = room.BigBlind * 50L;

        // 如果指定了筹码，验证其有效性
        if (bringChips > 0)
        {
            if (bringChips < minChips)
            {
                return (false, $"最低入场筹码为{minChips}");
            }
        }
        else
        {
            // 未指定筹码，使用默认值
            bringChips = Math.Min(user.Chips, minChips);
        }

        if (user.Chips < bringChips)
        {
            return (false, "筹码不足");
        }

        // 确保筹码不为负数
        bringChips = Math.Max(0, bringChips);

        // 找一个空座位
        var occupiedSeats = (await _roomPlayerRepository.GetListAsync(rp => rp.RoomId == room.Id))
            .Select(rp => rp.SeatIndex)
            .ToHashSet();

        // 加入房间（不自动入座，seatIndex = -1 表示未入座）
        var roomPlayer = new RoomPlayer
        {
            RoomId = room.Id,
            UserId = userId,
            SeatIndex = -1, // 未入座
            Chips = bringChips,
            IsReady = false,
            IsOnline = true
        };

        await _roomPlayerRepository.AddAsync(roomPlayer);

        return (true, "加入房间成功");
    }

    /// <summary>
    /// 离开房间
    /// </summary>
    public async Task<(bool Success, string Message)> LeaveRoomAsync(long userId, long roomId)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null)
        {
            return (false, "房间不存在");
        }

        var roomPlayer = await _roomPlayerRepository.FirstOrDefaultAsync(rp => rp.RoomId == roomId && rp.UserId == userId);
        if (roomPlayer == null)
        {
            return (false, "您不在此房间中");
        }

        // 如果是房主离开，解散房间
        if (room.OwnerId == userId)
        {
            return await DismissRoomAsync(userId, roomId);
        }

        // 普通玩家离开
        await _roomPlayerRepository.DeleteAsync(roomPlayer.Id);

        return (true, "已离开房间");
    }

    /// <summary>
    /// 准备/取消准备
    /// </summary>
    public async Task<(bool Success, string Message)> ToggleReadyAsync(long userId, long roomId)
    {
        var roomPlayer = await _roomPlayerRepository.FirstOrDefaultAsync(rp => rp.RoomId == roomId && rp.UserId == userId);
        if (roomPlayer == null)
        {
            return (false, "您不在此房间中");
        }

        roomPlayer.IsReady = !roomPlayer.IsReady;
        await _roomPlayerRepository.UpdateAsync(roomPlayer);

        return (true, roomPlayer.IsReady ? "已准备" : "取消准备");
    }

    /// <summary>
    /// 踢出玩家
    /// </summary>
    public async Task<(bool Success, string Message)> KickPlayerAsync(long ownerId, long roomId, long targetUserId)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null)
        {
            return (false, "房间不存在");
        }

        if (room.OwnerId != ownerId)
        {
            return (false, "只有房主才能踢人");
        }

        if (ownerId == targetUserId)
        {
            return (false, "不能踢出自己");
        }

        var targetPlayer = await _roomPlayerRepository.FirstOrDefaultAsync(rp => rp.RoomId == roomId && rp.UserId == targetUserId);
        if (targetPlayer == null)
        {
            return (false, "目标玩家不在房间中");
        }

        await _roomPlayerRepository.DeleteAsync(targetPlayer.Id);

        return (true, "已踢出玩家");
    }

    /// <summary>
    /// 解散房间
    /// </summary>
    public async Task<(bool Success, string Message)> DismissRoomAsync(long ownerId, long roomId)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null)
        {
            return (false, "房间不存在");
        }

        if (room.OwnerId != ownerId)
        {
            return (false, "只有房主才能解散房间");
        }

        // 删除所有房间玩家
        var players = await _roomPlayerRepository.GetListAsync(rp => rp.RoomId == roomId);
        foreach (var player in players)
        {
            await _roomPlayerRepository.DeleteAsync(player.Id);
        }

        // 更新房间状态
        room.Status = RoomStatus.Finished;
        await _roomRepository.UpdateAsync(room);

        return (true, "房间已解散");
    }

    /// <summary>
    /// 获取房间内的玩家列表（返回所有在线玩家）
    /// </summary>
    public async Task<List<RoomPlayerDto>> GetRoomPlayersAsync(long roomId)
    {
        var players = await _roomPlayerRepository.GetListAsync(rp => rp.RoomId == roomId && rp.IsOnline);
        var room = await _roomRepository.GetByIdAsync(roomId);

        var result = new List<RoomPlayerDto>();
        foreach (var player in players)
        {
            var user = await _userRepository.GetByIdAsync(player.UserId);
            if (user != null)
            {
                result.Add(new RoomPlayerDto
                {
                    UserId = user.Id,
                    Username = user.Username,
                    Nickname = user.Nickname,
                    Avatar = user.Avatar,
                    SeatIndex = player.SeatIndex,
                    Chips = player.Chips,
                    IsReady = player.IsReady,
                    IsOnline = player.IsOnline,
                    IsOwner = room != null && room.OwnerId == user.Id
                });
            }
        }

        return result.OrderBy(p => p.SeatIndex).ToList();
    }

    /// <summary>
    /// 换位置
    /// </summary>
    public async Task<(bool Success, string Message)> ChangeSeatAsync(long userId, long roomId, int newSeatIndex)
    {
        var room = await _roomRepository.GetByIdAsync(roomId);
        if (room == null)
        {
            return (false, "房间不存在");
        }

        if (newSeatIndex < 0 || newSeatIndex >= room.MaxPlayers)
        {
            return (false, "无效的座位号");
        }

        var roomPlayer = await _roomPlayerRepository.FirstOrDefaultAsync(rp => rp.RoomId == roomId && rp.UserId == userId);
        if (roomPlayer == null)
        {
            return (false, "您不在此房间中");
        }

        // 检查目标座位是否已被占用
        var targetSeatOccupied = await _roomPlayerRepository.AnyAsync(rp => rp.RoomId == roomId && rp.SeatIndex == newSeatIndex);
        if (targetSeatOccupied)
        {
            return (false, "该座位已被占用");
        }

        // 更新座位
        roomPlayer.SeatIndex = newSeatIndex;
        await _roomPlayerRepository.UpdateAsync(roomPlayer);

        return (true, "换位置成功");
    }

    /// <summary>
    /// 生成房间号
    /// </summary>
    private async Task<string> GenerateRoomCodeAsync()
    {
        var random = new Random();
        string roomCode;
        bool exists;

        do
        {
            roomCode = random.Next(100000, 999999).ToString();
            exists = await _roomRepository.AnyAsync(r => r.RoomCode == roomCode);
        } while (exists);

        return roomCode;
    }

    /// <summary>
    /// 映射到 DTO
    /// </summary>
    private async Task<RoomDto> MapToDtoAsync(Room room)
    {
        var owner = await _userRepository.GetByIdAsync(room.OwnerId);
        var players = await GetRoomPlayersAsync(room.Id);

        return new RoomDto
        {
            Id = room.Id,
            RoomCode = room.RoomCode,
            OwnerId = room.OwnerId,
            OwnerNickname = owner?.Nickname ?? "未知",
            MaxPlayers = room.MaxPlayers,
            SmallBlind = room.SmallBlind,
            BigBlind = room.BigBlind,
            Status = (int)room.Status,
            Players = players
        };
    }
}
