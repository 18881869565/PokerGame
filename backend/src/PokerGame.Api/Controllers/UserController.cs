using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokerGame.Api.Controllers;
using PokerGame.Application.Interfaces;
using PokerGame.Application.DTOs;

namespace PokerGame.Api.Controllers;

/// <summary>
/// 用户控制器
/// </summary>
[Authorize]
public class UserController : BaseController
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var user = await _userService.GetByIdAsync(CurrentUserId);
        if (user == null)
        {
            return Fail("用户不存在");
        }

        return Success(user);
    }

    /// <summary>
    /// 更新用户信息
    /// </summary>
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var dto = new UpdateProfileDto
        {
            Nickname = request.Nickname,
            Avatar = request.Avatar
        };

        var (success, message) = await _userService.UpdateProfileAsync(CurrentUserId, dto);

        if (!success)
        {
            return Fail(message);
        }

        return Success(message);
    }

    /// <summary>
    /// 领取每日筹码
    /// </summary>
    [HttpPost("daily-gift")]
    public async Task<IActionResult> DailyGift()
    {
        var (success, message, result) = await _userService.ClaimDailyGiftAsync(CurrentUserId);

        if (!success)
        {
            return Fail(message);
        }

        return Success(new { result!.GiftAmount, result.TotalChips }, message);
    }
}

/// <summary>
/// 更新用户信息请求
/// </summary>
public class UpdateProfileRequest
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
