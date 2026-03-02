using PokerGame.Application.DTOs;

namespace PokerGame.Application.Interfaces;

/// <summary>
/// 好友服务接口
/// </summary>
public interface IFriendService
{
    /// <summary>
    /// 发送好友请求
    /// </summary>
    Task<(bool Success, string Message)> SendRequestAsync(long userId, string target);

    /// <summary>
    /// 接受好友请求
    /// </summary>
    Task<(bool Success, string Message)> AcceptRequestAsync(long userId, long requestId);

    /// <summary>
    /// 拒绝好友请求
    /// </summary>
    Task<(bool Success, string Message)> RejectRequestAsync(long userId, long requestId);

    /// <summary>
    /// 删除好友
    /// </summary>
    Task<(bool Success, string Message)> DeleteFriendAsync(long userId, long friendId);

    /// <summary>
    /// 获取好友列表
    /// </summary>
    Task<List<FriendDto>> GetFriendsAsync(long userId);

    /// <summary>
    /// 获取收到的好友请求列表
    /// </summary>
    Task<List<FriendRequestDto>> GetPendingRequestsAsync(long userId);

    /// <summary>
    /// 检查是否是好友
    /// </summary>
    Task<bool> IsFriendAsync(long userId, long targetUserId);
}
