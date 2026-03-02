using PokerGame.Application.DTOs;

namespace PokerGame.Application.Interfaces;

/// <summary>
/// 用户服务接口
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 根据ID获取用户信息
    /// </summary>
    Task<UserDto?> GetByIdAsync(long userId);

    /// <summary>
    /// 根据用户名获取用户信息
    /// </summary>
    Task<UserDto?> GetByUsernameAsync(string username);

    /// <summary>
    /// 更新用户信息
    /// </summary>
    Task<(bool Success, string Message)> UpdateProfileAsync(long userId, UpdateProfileDto dto);

    /// <summary>
    /// 领取每日筹码
    /// </summary>
    Task<(bool Success, string Message, DailyGiftResultDto? Result)> ClaimDailyGiftAsync(long userId);

    /// <summary>
    /// 检查用户名是否存在
    /// </summary>
    Task<bool> UsernameExistsAsync(string username);
}
