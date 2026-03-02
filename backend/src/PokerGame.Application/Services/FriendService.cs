using PokerGame.Application.DTOs;
using PokerGame.Application.Interfaces;
using PokerGame.Domain.Entities;
using PokerGame.Domain.Enums;
using PokerGame.Infrastructure.Repository;

namespace PokerGame.Application.Services;

/// <summary>
/// 好友服务实现
/// </summary>
public class FriendService : IFriendService
{
    private readonly IRepository<Friend> _friendRepository;
    private readonly IRepository<User> _userRepository;

    public FriendService(IRepository<Friend> friendRepository, IRepository<User> userRepository)
    {
        _friendRepository = friendRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// 发送好友请求
    /// </summary>
    public async Task<(bool Success, string Message)> SendRequestAsync(long userId, string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return (false, "请输入目标用户名");
        }

        // 查找目标用户
        var targetUser = await _userRepository.FirstOrDefaultAsync(u => u.Username == target);
        if (targetUser == null)
        {
            // 尝试作为ID查找
            if (long.TryParse(target, out var targetId))
            {
                targetUser = await _userRepository.GetByIdAsync(targetId);
            }
        }

        if (targetUser == null)
        {
            return (false, "目标用户不存在");
        }

        if (targetUser.Id == userId)
        {
            return (false, "不能添加自己为好友");
        }

        // 检查是否已有好友关系
        var existingFriend = await _friendRepository.FirstOrDefaultAsync(
            f => (f.UserId == userId && f.FriendId == targetUser.Id) ||
                 (f.UserId == targetUser.Id && f.FriendId == userId));

        if (existingFriend != null)
        {
            return existingFriend.Status switch
            {
                FriendStatus.Accepted => (false, "你们已经是好友了"),
                FriendStatus.Pending when existingFriend.UserId == userId => (false, "好友请求已发送，请等待对方确认"),
                FriendStatus.Pending => (false, "对方已向你发送好友请求，请查看"),
                _ => (false, "好友关系异常")
            };
        }

        // 创建好友请求
        var friend = new Friend
        {
            UserId = userId,
            FriendId = targetUser.Id,
            Status = FriendStatus.Pending,
            CreatedAt = DateTime.Now
        };

        await _friendRepository.AddAsync(friend);

        return (true, "好友请求已发送");
    }

    /// <summary>
    /// 接受好友请求
    /// </summary>
    public async Task<(bool Success, string Message)> AcceptRequestAsync(long userId, long requestId)
    {
        var friendRequest = await _friendRepository.GetByIdAsync(requestId);
        if (friendRequest == null)
        {
            return (false, "好友请求不存在");
        }

        // 验证是否是发给当前用户的请求
        if (friendRequest.FriendId != userId || friendRequest.Status != FriendStatus.Pending)
        {
            return (false, "无效的好友请求");
        }

        // 更新状态
        friendRequest.Status = FriendStatus.Accepted;
        await _friendRepository.UpdateAsync(friendRequest);

        return (true, "已接受好友请求");
    }

    /// <summary>
    /// 拒绝好友请求
    /// </summary>
    public async Task<(bool Success, string Message)> RejectRequestAsync(long userId, long requestId)
    {
        var friendRequest = await _friendRepository.GetByIdAsync(requestId);
        if (friendRequest == null)
        {
            return (false, "好友请求不存在");
        }

        // 验证是否是发给当前用户的请求
        if (friendRequest.FriendId != userId || friendRequest.Status != FriendStatus.Pending)
        {
            return (false, "无效的好友请求");
        }

        // 更新状态为拒绝
        friendRequest.Status = FriendStatus.Rejected;
        await _friendRepository.UpdateAsync(friendRequest);

        return (true, "已拒绝好友请求");
    }

    /// <summary>
    /// 删除好友
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteFriendAsync(long userId, long friendId)
    {
        // 查找好友关系（双向查找）
        var friendRelation = await _friendRepository.FirstOrDefaultAsync(
            f => f.Status == FriendStatus.Accepted &&
                 ((f.UserId == userId && f.FriendId == friendId) ||
                  (f.UserId == friendId && f.FriendId == userId)));

        if (friendRelation == null)
        {
            return (false, "好友关系不存在");
        }

        await _friendRepository.DeleteAsync(friendRelation.Id);

        return (true, "已删除好友");
    }

    /// <summary>
    /// 获取好友列表
    /// </summary>
    public async Task<List<FriendDto>> GetFriendsAsync(long userId)
    {
        var friends = await _friendRepository.GetListAsync(
            f => f.Status == FriendStatus.Accepted &&
                 (f.UserId == userId || f.FriendId == userId));

        var result = new List<FriendDto>();

        foreach (var friend in friends)
        {
            // 确定好友ID（如果当前用户是UserId，则好友是FriendId，反之亦然）
            var friendUserId = friend.UserId == userId ? friend.FriendId : friend.UserId;
            var friendUser = await _userRepository.GetByIdAsync(friendUserId);

            if (friendUser != null)
            {
                result.Add(new FriendDto
                {
                    Id = friend.Id,
                    UserId = friendUser.Id,
                    Username = friendUser.Username,
                    Nickname = friendUser.Nickname,
                    Avatar = friendUser.Avatar,
                    Status = (int)friend.Status,
                    IsOnline = false // TODO: 实现在线状态检测
                });
            }
        }

        return result;
    }

    /// <summary>
    /// 获取收到的好友请求列表
    /// </summary>
    public async Task<List<FriendRequestDto>> GetPendingRequestsAsync(long userId)
    {
        var requests = await _friendRepository.GetListAsync(
            f => f.FriendId == userId && f.Status == FriendStatus.Pending);

        var result = new List<FriendRequestDto>();

        foreach (var request in requests)
        {
            var fromUser = await _userRepository.GetByIdAsync(request.UserId);
            if (fromUser != null)
            {
                result.Add(new FriendRequestDto
                {
                    Id = request.Id,
                    FromUserId = fromUser.Id,
                    FromUsername = fromUser.Username,
                    FromNickname = fromUser.Nickname,
                    FromAvatar = fromUser.Avatar,
                    CreatedAt = request.CreatedAt
                });
            }
        }

        return result.OrderByDescending(r => r.CreatedAt).ToList();
    }

    /// <summary>
    /// 检查是否是好友
    /// </summary>
    public async Task<bool> IsFriendAsync(long userId, long targetUserId)
    {
        return await _friendRepository.AnyAsync(
            f => f.Status == FriendStatus.Accepted &&
                 ((f.UserId == userId && f.FriendId == targetUserId) ||
                  (f.UserId == targetUserId && f.FriendId == userId)));
    }
}
