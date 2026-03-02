using PokerGame.Application.DTOs;

namespace PokerGame.Application.Interfaces;

/// <summary>
/// 认证服务接口
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// 用户注册
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <param name="nickname">昵称（可选）</param>
    /// <returns>注册结果</returns>
    Task<(bool Success, string Message, UserDto? User)> RegisterAsync(string username, string password, string? nickname);

    /// <summary>
    /// 用户登录
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <returns>登录结果（包含 Token 和用户信息）</returns>
    Task<(bool Success, string Message, LoginResultDto? Result)> LoginAsync(string username, string password);

    /// <summary>
    /// 生成 JWT Token
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="username">用户名</param>
    /// <returns>JWT Token</returns>
    string GenerateJwtToken(long userId, string username);
}
