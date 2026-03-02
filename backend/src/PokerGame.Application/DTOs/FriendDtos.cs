namespace PokerGame.Application.DTOs;

/// <summary>
/// 好友信息 DTO
/// </summary>
public class FriendDto
{
    /// <summary>
    /// 好友关系ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 好友用户ID
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// 好友用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 好友昵称
    /// </summary>
    public string Nickname { get; set; } = string.Empty;

    /// <summary>
    /// 好友头像
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// 好友状态 (0-待确认, 1-已接受, 2-已拒绝)
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// 是否是在线
    /// </summary>
    public bool IsOnline { get; set; }
}

/// <summary>
/// 好友请求 DTO（收到的请求）
/// </summary>
public class FriendRequestDto
{
    /// <summary>
    /// 关系ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 请求者用户ID
    /// </summary>
    public long FromUserId { get; set; }

    /// <summary>
    /// 请求者用户名
    /// </summary>
    public string FromUsername { get; set; } = string.Empty;

    /// <summary>
    /// 请求者昵称
    /// </summary>
    public string FromNickname { get; set; } = string.Empty;

    /// <summary>
    /// 请求者头像
    /// </summary>
    public string? FromAvatar { get; set; }

    /// <summary>
    /// 请求时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
