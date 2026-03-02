using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PokerGame.Api.Controllers;
using PokerGame.Application.Interfaces;

namespace PokerGame.Api.Controllers;

/// <summary>
/// 好友控制器
/// </summary>
[Authorize]
[Route("api/friend")]
public class FriendController : BaseController
{
    private readonly IFriendService _friendService;

    public FriendController(IFriendService friendService)
    {
        _friendService = friendService;
    }

    /// <summary>
    /// 发送好友请求
    /// </summary>
    [HttpPost("request")]
    public async Task<IActionResult> SendRequest([FromBody] FriendRequest request)
    {
        var (success, message) = await _friendService.SendRequestAsync(CurrentUserId, request.Target);

        if (!success)
        {
            return Fail(message);
        }

        return Success(message);
    }

    /// <summary>
    /// 接受好友请求
    /// </summary>
    [HttpPost("accept/{id}")]
    public async Task<IActionResult> AcceptRequest(long id)
    {
        var (success, message) = await _friendService.AcceptRequestAsync(CurrentUserId, id);

        if (!success)
        {
            return Fail(message);
        }

        return Success(message);
    }

    /// <summary>
    /// 拒绝好友请求
    /// </summary>
    [HttpPost("reject/{id}")]
    public async Task<IActionResult> RejectRequest(long id)
    {
        var (success, message) = await _friendService.RejectRequestAsync(CurrentUserId, id);

        if (!success)
        {
            return Fail(message);
        }

        return Success(message);
    }

    /// <summary>
    /// 删除好友
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFriend(long id)
    {
        var (success, message) = await _friendService.DeleteFriendAsync(CurrentUserId, id);

        if (!success)
        {
            return Fail(message);
        }

        return Success(message);
    }

    /// <summary>
    /// 获取好友列表
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> GetList()
    {
        var friends = await _friendService.GetFriendsAsync(CurrentUserId);
        return Success(friends);
    }

    /// <summary>
    /// 获取收到的好友请求
    /// </summary>
    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests()
    {
        var requests = await _friendService.GetPendingRequestsAsync(CurrentUserId);
        return Success(requests);
    }
}

/// <summary>
/// 好友请求
/// </summary>
public class FriendRequest
{
    /// <summary>
    /// 目标用户ID或用户名
    /// </summary>
    public string Target { get; set; } = string.Empty;
}
