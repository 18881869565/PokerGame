using PokerGame.Application.DTOs;
using PokerGame.Application.Interfaces;
using PokerGame.Domain.Entities;
using PokerGame.Infrastructure.Repository;

namespace PokerGame.Application.Services;

/// <summary>
/// 用户服务实现
/// </summary>
public class UserService : IUserService
{
    private readonly IRepository<User> _userRepository;

    public UserService(IRepository<User> userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// 根据ID获取用户信息
    /// </summary>
    public async Task<UserDto?> GetByIdAsync(long userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user == null ? null : MapToDto(user);
    }

    /// <summary>
    /// 根据用户名获取用户信息
    /// </summary>
    public async Task<UserDto?> GetByUsernameAsync(string username)
    {
        var user = await _userRepository.FirstOrDefaultAsync(u => u.Username == username);
        return user == null ? null : MapToDto(user);
    }

    /// <summary>
    /// 更新用户信息
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateProfileAsync(long userId, UpdateProfileDto dto)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return (false, "用户不存在");
        }

        // 更新昵称
        if (!string.IsNullOrWhiteSpace(dto.Nickname))
        {
            if (dto.Nickname.Length > 50)
            {
                return (false, "昵称长度不能超过50个字符");
            }
            user.Nickname = dto.Nickname;
        }

        // 更新头像
        if (dto.Avatar != null)
        {
            user.Avatar = dto.Avatar;
        }

        user.UpdatedAt = DateTime.Now;
        await _userRepository.UpdateAsync(user);

        return (true, "更新成功");
    }

    /// <summary>
    /// 领取每日筹码
    /// </summary>
    public async Task<(bool Success, string Message, DailyGiftResultDto? Result)> ClaimDailyGiftAsync(long userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return (false, "用户不存在", null);
        }

        // 检查今天是否已领取
        if (user.DailyGiftAt.HasValue && user.DailyGiftAt.Value.Date == DateTime.Today)
        {
            return (false, "今日已领取，请明天再来", null);
        }

        // 计算赠送筹码（基础1000 + 连续登录奖励）
        var giftAmount = 1000L;

        // 如果昨天有领取，增加连续登录奖励
        if (user.DailyGiftAt.HasValue && user.DailyGiftAt.Value.Date == DateTime.Today.AddDays(-1))
        {
            giftAmount += 200; // 连续登录奖励
        }

        user.Chips += giftAmount;
        user.DailyGiftAt = DateTime.Now;
        user.UpdatedAt = DateTime.Now;

        await _userRepository.UpdateAsync(user);

        return (true, $"领取成功，获得 {giftAmount} 筹码", new DailyGiftResultDto
        {
            GiftAmount = giftAmount,
            TotalChips = user.Chips
        });
    }

    /// <summary>
    /// 检查用户名是否存在
    /// </summary>
    public async Task<bool> UsernameExistsAsync(string username)
    {
        return await _userRepository.AnyAsync(u => u.Username == username);
    }

    /// <summary>
    /// 映射到 DTO
    /// </summary>
    private static UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Nickname = user.Nickname,
            Avatar = user.Avatar,
            Chips = user.Chips
        };
    }
}
