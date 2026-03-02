using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PokerGame.Application.DTOs;
using PokerGame.Application.Interfaces;
using PokerGame.Domain.Entities;
using PokerGame.Infrastructure.Repository;

namespace PokerGame.Application.Services;

/// <summary>
/// 认证服务实现
/// </summary>
public class AuthService : IAuthService
{
    private readonly IRepository<User> _userRepository;
    private readonly IConfiguration _configuration;

    public AuthService(IRepository<User> userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    public async Task<(bool Success, string Message, UserDto? User)> RegisterAsync(string username, string password, string? nickname)
    {
        // 验证用户名
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 50)
        {
            return (false, "用户名长度必须在3-50个字符之间", null);
        }

        // 验证密码
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            return (false, "密码长度必须至少6个字符", null);
        }

        // 检查用户名是否已存在
        var exists = await _userRepository.AnyAsync(u => u.Username == username);
        if (exists)
        {
            return (false, "用户名已被使用", null);
        }

        // 创建用户
        var user = new User
        {
            Username = username,
            PasswordHash = HashPassword(password),
            Nickname = string.IsNullOrWhiteSpace(nickname) ? username : nickname,
            Chips = 10000, // 初始筹码
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        await _userRepository.AddAsync(user);

        // 重新获取用户以获得自增ID
        var createdUser = await _userRepository.FirstOrDefaultAsync(u => u.Username == username);

        return (true, "注册成功", MapToDto(createdUser!));
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    public async Task<(bool Success, string Message, LoginResultDto? Result)> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return (false, "用户名和密码不能为空", null);
        }

        // 查找用户
        var user = await _userRepository.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            return (false, "用户名或密码错误", null);
        }

        // 验证密码
        if (!VerifyPassword(password, user.PasswordHash))
        {
            return (false, "用户名或密码错误", null);
        }

        // 生成 Token
        var token = GenerateJwtToken(user.Id, user.Username);

        return (true, "登录成功", new LoginResultDto
        {
            Token = token,
            User = MapToDto(user)
        });
    }

    /// <summary>
    /// 生成 JWT Token
    /// </summary>
    public string GenerateJwtToken(long userId, string username)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? "DefaultSecretKeyForDevelopment123456";
        var issuer = _configuration["Jwt:Issuer"] ?? "PokerGame";
        var audience = _configuration["Jwt:Audience"] ?? "PokerGameUsers";
        var expirationHours = int.Parse(_configuration["Jwt:ExpirationHours"] ?? "24");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, username),
            new Claim("id", userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expirationHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// 密码哈希
    /// </summary>
    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "PokerGameSalt"));
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// 验证密码
    /// </summary>
    private static bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
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
