using Microsoft.AspNetCore.Mvc;
using PokerGame.Api.Controllers;
using PokerGame.Application.Interfaces;

namespace PokerGame.Api.Controllers;

/// <summary>
/// 认证控制器
/// </summary>
public class AuthController : BaseController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Fail("用户名和密码不能为空");
        }

        var (success, message, user) = await _authService.RegisterAsync(request.Username, request.Password, request.Nickname);

        if (!success)
        {
            return Fail(message);
        }

        return Success(new { UserId = user!.Id }, message);
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Fail("用户名和密码不能为空");
        }

        var (success, message, result) = await _authService.LoginAsync(request.Username, request.Password);

        if (!success)
        {
            return Fail(message);
        }

        return Success(new { Token = result!.Token, User = result.User }, message);
    }
}

/// <summary>
/// 注册请求
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 昵称
    /// </summary>
    public string? Nickname { get; set; }
}

/// <summary>
/// 登录请求
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
