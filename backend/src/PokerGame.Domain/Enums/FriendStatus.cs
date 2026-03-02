namespace PokerGame.Domain.Enums;

/// <summary>
/// 好友状态
/// </summary>
public enum FriendStatus
{
    /// <summary>
    /// 待确认
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 已接受
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// 已拒绝
    /// </summary>
    Rejected = 2
}
