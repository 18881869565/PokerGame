namespace PokerGame.Application.DTOs;

/// <summary>
/// 用户信息 DTO
/// </summary>
public class UserDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 昵称
    /// </summary>
    public string Nickname { get; set; } = string.Empty;

    /// <summary>
    /// 头像URL
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 筹码数量
    /// </summary>
    public long Chips { get; set; }
}

/// <summary>
/// 登录结果 DTO
/// </summary>
public class LoginResultDto
{
    /// <summary>
    /// JWT Token
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 用户信息
    /// </summary>
    public UserDto User { get; set; } = null!;
}

/// <summary>
/// 更新用户信息请求
/// </summary>
public class UpdateProfileDto
{
    /// <summary>
    /// 昵称
    /// </summary>
    public string? Nickname { get; set; }

    /// <summary>
    /// 头像URL
    /// </summary>
    public string? Avatar { get; set; }
}

/// <summary>
/// 每日领取结果 DTO
/// </summary>
public class DailyGiftResultDto
{
    /// <summary>
    /// 领取的筹码数量
    /// </summary>
    public long GiftAmount { get; set; }

    /// <summary>
    /// 当前总筹码
    /// </summary>
    public long TotalChips { get; set; }
}
